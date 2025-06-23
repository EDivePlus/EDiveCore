using UnityEditor.Localization.Reporting;
using UnityEngine;

namespace EDIVE.Localization.Editor
{
    public class BatchProgressBarReporter : ProgressBarReporter
    {
        private int TotalBatchCount { get; set; }
        private int CurrentBatchIndex { get; set; }
        private int CurrentBatchSize { get; set; }
        
        private int CurrentIndexWithinBatch { get; set; }
        
        public BatchProgressBarReporter(int totalBatchCount)
        {
            TotalBatchCount = totalBatchCount;
        }

        public void SetBatch(int batchIndex, int batchSize)
        {
            CurrentBatchIndex = batchIndex;
            CurrentBatchSize = Mathf.Max(batchSize, 1);
        }

        public void SetIndexWithinBatch(int index)
        {
            CurrentIndexWithinBatch = index;
        }

        protected override void PrintStatus(string title, string description, float progress)
        {
            base.PrintStatus(title, description, GetTotalProgress(progress));
        }
        
        private float GetTotalProgress(float currentProgress)
        {
            var currentBatchProgress = GetCurrentBatchProgress(currentProgress);
            return (CurrentBatchIndex + Mathf.Clamp01(currentBatchProgress)) / Mathf.Max(TotalBatchCount, 1);
        }
        
        private float GetCurrentBatchProgress(float currentProgress)
        {
            return (CurrentIndexWithinBatch + Mathf.Clamp01(currentProgress)) / Mathf.Max(CurrentBatchSize, 1);
        }
    }
}
