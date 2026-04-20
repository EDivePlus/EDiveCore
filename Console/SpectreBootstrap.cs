using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using UnityEngine;

namespace EDIVE.Utils
{
    public static class SpectreBootstrap
    {
        private static readonly ConcurrentQueue<LogEntry> _pendingOutput = new();
        private static readonly List<string> _history = new();
        private static readonly StringBuilder _currentInput = new();
        private static readonly Dictionary<string, ConsoleCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _commandsLock = new();
        private static readonly object _writeLock = new();
        private static readonly ManualResetEventSlim _renderPaused = new(true);

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
#if !UNITY_EDITOR
            Initialize();
#endif
        }

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
            }
            catch { }

#if UNITY_STANDALONE_WIN
            WindowsConsole.Configure();
#endif
            Debug.unityLogger.logHandler = new CapturingLogHandler();
            Console.Out.Flush();

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
                lock (_writeLock)
                {
                    Console.Write(CURSOR_TO_COL0 + ERASE_LINE);
                    Console.Write(SHOW_CURSOR);
                    Console.Out.Flush();
                }
            }
            catch { }
        }

        public static void RegisterCommand(ConsoleCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            lock (_commandsLock)
                _commands[command.Name] = command;
        }

        public static void RegisterCommands(params ConsoleCommand[] commands)
        {
            foreach (var c in commands) RegisterCommand(c);
        }

        public static bool UnregisterCommand(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (_commandsLock)
                return _commands.Remove(name.Trim().ToLowerInvariant());
        }

        public static IReadOnlyCollection<ConsoleCommand> GetCommands()
        {
            lock (_commandsLock)
                return new List<ConsoleCommand>(_commands.Values);
        }

        public static void AppendLog(string markup)
        {
            var timestamp = $"[grey58]{DateTime.Now:HH:mm:ss}[/]";
            _pendingOutput.Enqueue(LogEntry.FromMarkup($"{timestamp} {markup}"));
        }

        public static void AppendRenderable(IRenderable renderable)
        {
            if (renderable == null) return;
            _pendingOutput.Enqueue(LogEntry.FromRenderable(renderable));
        }

        

       

        private static async UniTaskVoid RunConsoleLoop(CancellationToken ct)
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    _renderPaused.Wait(ct);

                    try
                    {
                        while (Console.KeyAvailable)
                            HandleKey(Console.ReadKey(intercept: true));

                        Redraw();
                    }
                    catch (Exception ex)
                    {
                        _pendingOutput.Enqueue(LogEntry.FromMarkup($"[red]Render error:[/] {Markup.Escape(ex.Message)}"));
                    }

                    Thread.Sleep(FRAME_INTERVAL_MS);
                }
            }
            catch { }
        }

        private static void Redraw()
        {
            lock (_writeLock)
            {
                var sb = new StringBuilder();
                sb.Append(HIDE_CURSOR);
                sb.Append(CURSOR_TO_COL0);
                sb.Append(ERASE_LINE);

                while (_pendingOutput.TryDequeue(out var entry))
                {
                    var text = entry.IsRenderable
                        ? RenderRenderable(entry.Renderable)
                        : RenderMarkup(entry.Markup);
                    sb.Append(text.TrimEnd('\r', '\n'));
                    sb.Append('\n');
                }

                sb.Append(RenderMarkup($"[bold green]>[/] {Markup.Escape(_currentInput.ToString())}"));

                Console.Write(sb.ToString());
                Console.Out.Flush();
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
            lock (_writeLock)
            {
                _renderPaused.Reset();
                Console.Write(CURSOR_TO_COL0 + ERASE_LINE + SHOW_CURSOR);
                Console.Out.Flush();
            }

            try
            {
                action();
            }
            finally
            {
                lock (_writeLock)
                {
                    Console.Write(HIDE_CURSOR);
                    Console.Out.Flush();
                    _renderPaused.Set();
                }
            }
        }

        public static async UniTask ExclusiveAsync(Func<UniTask> action)
        {
            lock (_writeLock)
            {
                _renderPaused.Reset();
                Console.Write(CURSOR_TO_COL0 + ERASE_LINE + SHOW_CURSOR);
                Console.Out.Flush();
            }
            try
            {
                await action();
            }
            finally
            {
                lock (_writeLock)
                {
                    Console.Write(HIDE_CURSOR);
                    Console.Out.Flush();
                    _renderPaused.Set();
                }
            }
        }

        private static void HandleKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    var cmd = _currentInput.ToString().Trim();
                    _currentInput.Clear();
                    _historyIndex = -1;

                    if (!string.IsNullOrEmpty(cmd))
                    {
                        _history.Add(cmd);
                        DispatchCommand(cmd);
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (_currentInput.Length > 0)
                        _currentInput.Length--;
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex == -1) _historyIndex = _history.Count;
                        _historyIndex = Math.Max(0, _historyIndex - 1);
                        _currentInput.Clear();
                        _currentInput.Append(_history[_historyIndex]);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_history.Count > 0 && _historyIndex != -1)
                    {
                        _historyIndex++;
                        if (_historyIndex >= _history.Count)
                        {
                            _historyIndex = -1;
                            _currentInput.Clear();
                        }
                        else
                        {
                            _currentInput.Clear();
                            _currentInput.Append(_history[_historyIndex]);
                        }
                    }
                    break;

                case ConsoleKey.Escape:
                    _currentInput.Clear();
                    _historyIndex = -1;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                        _currentInput.Append(key.KeyChar);
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
            lock (_commandsLock)
                _commands.TryGetValue(name, out command);

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
            lock (_writeLock)
            {
                Console.Write("\u001b[2J\u001b[H");
                Console.Out.Flush();
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

        public static bool Confirm(string question)
        {
            var result = false;
            
            return result;
        }

        public static string Ask(string question)
        {
            return Ask<string>(question);
        }

        public static T Ask<T>(string question)
        {
            T result = default;
            Exclusive(() => result = AnsiConsole.Ask<T>(question));
            return result;
        }
    }
}
