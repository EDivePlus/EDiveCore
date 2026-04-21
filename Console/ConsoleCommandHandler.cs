#if SPECTRE_CONSOLE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using Spectre.Console;
using Spectre.Console.Rendering;
using UnityEngine;

namespace EDIVE.Console
{
    public static class ConsoleCommandHandler
    {
        private static readonly ConcurrentQueue<LogEntry> PENDING_OUTPUT = new();
        private static readonly List<string> HISTORY = new();
        private static readonly StringBuilder CURRENT_INPUT = new();
        private static readonly Dictionary<string, ConsoleCommand> COMMANDS = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object COMMANDS_LOCK = new();
        private static readonly object WRITE_LOCK = new();
        private static readonly ManualResetEventSlim RENDER_PAUSED = new(true);

        private static CancellationTokenSource _cts;
        private static int _historyIndex = -1;
        private static bool _initialized;

        private const int FRAME_INTERVAL_MS = 50;
        private const string ERASE_LINE = "\u001b[2K";
        private const string CURSOR_TO_COL0 = "\r";
        private const string HIDE_CURSOR = "\u001b[?25l";
        private const string SHOW_CURSOR = "\u001b[?25h";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnLoad()
        {
            if (PlatformUtils.IsHeadless())
                Initialize();
        }

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                System.Console.OutputEncoding = Encoding.UTF8;
                System.Console.InputEncoding = Encoding.UTF8;
            }
            catch { }

#if UNITY_STANDALONE_WIN
            WindowsConsoleHelper.Configure();
#endif
            _cts = new CancellationTokenSource();
            Application.quitting += OnQuitting;

            AppendLog("[green]Server console started. Type 'help' for commands.[/]");

            RunConsoleLoop(_cts.Token).Forget();
        }

        private static void OnQuitting()
        {
            _cts?.Cancel();
            try
            {
                lock (WRITE_LOCK)
                {
                    System.Console.Write(CURSOR_TO_COL0 + ERASE_LINE);
                    System.Console.Write(SHOW_CURSOR);
                    System.Console.Out.Flush();
                }
            }
            catch { }
        }

        public static void RegisterCommand(ConsoleCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            lock (COMMANDS_LOCK)
                COMMANDS[command.Name] = command;
        }

        public static void RegisterCommands(params ConsoleCommand[] commands)
        {
            foreach (var c in commands) RegisterCommand(c);
        }

        public static bool UnregisterCommand(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (COMMANDS_LOCK)
                return COMMANDS.Remove(name.Trim().ToLowerInvariant());
        }

        public static IReadOnlyCollection<ConsoleCommand> GetCommands()
        {
            lock (COMMANDS_LOCK)
                return new List<ConsoleCommand>(COMMANDS.Values);
        }

        public static void AppendLog(string markup)
        {
            var timestamp = $"[grey58]{DateTime.Now:u}[/]";
            PENDING_OUTPUT.Enqueue(LogEntry.FromMarkup($"{timestamp} {markup}"));
        }

        public static void AppendRenderable(IRenderable renderable)
        {
            if (renderable == null) return;
            PENDING_OUTPUT.Enqueue(LogEntry.FromRenderable(renderable));
        }

        private static async UniTaskVoid RunConsoleLoop(CancellationToken ct)
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    RENDER_PAUSED.Wait(ct);

                    try
                    {
                        while (System.Console.KeyAvailable)
                            HandleKey(System.Console.ReadKey(intercept: true));

                        Redraw();
                    }
                    catch (Exception ex)
                    {
                        PENDING_OUTPUT.Enqueue(LogEntry.FromMarkup($"[red]Render error:[/] {Markup.Escape(ex.Message)}"));
                    }

                    Thread.Sleep(FRAME_INTERVAL_MS);
                }
            }
            catch (OperationCanceledException) { }
        }

        private static void Redraw()
        {
            lock (WRITE_LOCK)
            {
                var sb = new StringBuilder();
                sb.Append(HIDE_CURSOR);
                sb.Append(CURSOR_TO_COL0);
                sb.Append(ERASE_LINE);

                while (PENDING_OUTPUT.TryDequeue(out var entry))
                {
                    var text = entry.IsRenderable
                        ? RenderRenderable(entry.Renderable)
                        : RenderMarkup(entry.Markup);
                    sb.Append(text.TrimEnd('\r', '\n'));
                    sb.Append('\n');
                }

                sb.Append(RenderMarkup($"[bold green]>[/] {Markup.Escape(CURRENT_INPUT.ToString())}"));

                System.Console.Write(sb.ToString());
                System.Console.Out.Flush();
            }
        }

        private static IAnsiConsole CreateMarkupConsole(StringWriter writer) =>
            AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(writer),
                Ansi = AnsiSupport.Yes,
                ColorSystem = ColorSystemSupport.TrueColor,
            });

        private static string RenderMarkup(string markup)
        {
            try
            {
                var writer = new StringWriter();
                CreateMarkupConsole(writer).Markup(markup);
                return writer.ToString();
            }
            catch
            {
                return Regex.Replace(markup, @"\[/?[^\]]*\]", "");
            }
        }

        public static string RenderRenderable(IRenderable renderable)
        {
            try
            {
                var writer = new StringWriter();
                CreateMarkupConsole(writer).Write(renderable);
                return writer.ToString();
            }
            catch (Exception ex)
            {
                return $"<render error: {ex.Message}>";
            }
        }

        public static void Exclusive(Action action)
        {
            lock (WRITE_LOCK)
            {
                RENDER_PAUSED.Reset();
                System.Console.Write(CURSOR_TO_COL0 + ERASE_LINE + SHOW_CURSOR);
                System.Console.Out.Flush();
            }

            try
            {
                action();
            }
            finally
            {
                lock (WRITE_LOCK)
                {
                    System.Console.Write(HIDE_CURSOR);
                    System.Console.Out.Flush();
                    RENDER_PAUSED.Set();
                }
            }
        }

        public static async UniTask ExclusiveAsync(Func<UniTask> action)
        {
            lock (WRITE_LOCK)
            {
                RENDER_PAUSED.Reset();
                System.Console.Write(CURSOR_TO_COL0 + ERASE_LINE + SHOW_CURSOR);
                System.Console.Out.Flush();
            }
            try
            {
                await action();
            }
            finally
            {
                lock (WRITE_LOCK)
                {
                    System.Console.Write(HIDE_CURSOR);
                    System.Console.Out.Flush();
                    RENDER_PAUSED.Set();
                }
            }
        }

        private static void HandleKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    var cmd = CURRENT_INPUT.ToString().Trim();
                    CURRENT_INPUT.Clear();
                    _historyIndex = -1;

                    if (!string.IsNullOrEmpty(cmd))
                    {
                        HISTORY.Add(cmd);
                        DispatchCommand(cmd);
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (CURRENT_INPUT.Length > 0)
                        CURRENT_INPUT.Length--;
                    break;

                case ConsoleKey.UpArrow:
                    if (HISTORY.Count > 0)
                    {
                        if (_historyIndex == -1) _historyIndex = HISTORY.Count;
                        _historyIndex = Math.Max(0, _historyIndex - 1);
                        CURRENT_INPUT.Clear();
                        CURRENT_INPUT.Append(HISTORY[_historyIndex]);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (HISTORY.Count > 0 && _historyIndex != -1)
                    {
                        _historyIndex++;
                        if (_historyIndex >= HISTORY.Count)
                        {
                            _historyIndex = -1;
                            CURRENT_INPUT.Clear();
                        }
                        else
                        {
                            CURRENT_INPUT.Clear();
                            CURRENT_INPUT.Append(HISTORY[_historyIndex]);
                        }
                    }
                    break;

                case ConsoleKey.Escape:
                    CURRENT_INPUT.Clear();
                    _historyIndex = -1;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                        CURRENT_INPUT.Append(key.KeyChar);
                    break;
            }
        }

        private static void DispatchCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var name = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            ConsoleCommand command;
            lock (COMMANDS_LOCK)
                COMMANDS.TryGetValue(name, out command);

            if (command == null)
            {
                AppendLog($"[red]Unknown command:[/] {Markup.Escape(name)}");
                return;
            }

            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    await command.Handler(args);
                }
                catch (Exception ex)
                {
                    AppendLog($"[red]'{Markup.Escape(command.Name)}' failed:[/] {Markup.Escape(ex.Message)}");
                }
            }).Forget();
        }

        public static void ClearScreen()
        {
            lock (WRITE_LOCK)
            {
                System.Console.Write("\u001b[2J\u001b[H");
                System.Console.Out.Flush();
            }
        }

        public static UniTask OnMainThread(Action action)
        {
            var tcs = new UniTaskCompletionSource();
            UniTask.Post(() =>
            {
                try { action(); tcs.TrySetResult(); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });
            return tcs.Task;
        }
        
        public static T Select<T>(string title, IEnumerable<T> choices)
        {
            T result = default;
            Exclusive(() =>
            {
                result = AnsiConsole.Prompt(new SelectionPrompt<T>()
                    .Title(title)
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                    .AddChoices(choices));
            });
            return result;
        }

        public static List<T> MultiSelect<T>(string title, IEnumerable<T> choices)
        {
            List<T> result = null;
            Exclusive(() =>
            {
                result = AnsiConsole.Prompt(new MultiSelectionPrompt<T>()
                    .Title(title)
                    .PageSize(10)
                    .NotRequired()
                    .InstructionsText("[grey](<space> to toggle, <enter> to accept)[/]")
                    .AddChoices(choices));
            });
            return result ?? new List<T>();
        }
    }
}
#endif
