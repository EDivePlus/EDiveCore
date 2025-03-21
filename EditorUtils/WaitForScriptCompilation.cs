// Author: František Holubec
// Created: 17.03.2025

using UnityEditor.Compilation;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    public class WaitForScriptCompilation : CustomYieldInstruction
    {
        private bool _isCompilationRequested;
        private bool _isCompilationDone;

        public CompilerMessage[] CompilationMessages { get; private set; }

        public override bool keepWaiting
        {
            get
            {
                if (!_isCompilationRequested)
                {
                    _isCompilationRequested = true;
                    CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
                    CompilationPipeline.RequestScriptCompilation();
                }
                return !_isCompilationDone;
            }
        }

        private void OnCompilationFinished(string assembly, CompilerMessage[] messages)
        {
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
            CompilationMessages = messages;
            _isCompilationDone = true;
        }
    }
}
