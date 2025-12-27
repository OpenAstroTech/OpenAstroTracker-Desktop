namespace OATControl.ViewModels
{
	public class PlatesolveEventArgs
	{
		private float _raHours;
		private float _decDegrees;

		public PlatesolveEventArgs(float raHours, float decDegrees)
		{
			_raHours = raHours;
			_decDegrees = decDegrees;
		}
		public float RAHours { get { return _raHours; } }
		public float DecDegrees { get { return _decDegrees; } }
	}
}