using System;

namespace OATControl.ViewModels
{
    public interface IPolarAlignLogProcessor
    {
        event EventHandler<PolarAlignStatusEventArgs> StatusChanged;
        event EventHandler<PolarAlignCorrectionEventArgs> CorrectionRequired;
        void Start();
        void Stop();
		void LogfileWasReset();
	}
} 