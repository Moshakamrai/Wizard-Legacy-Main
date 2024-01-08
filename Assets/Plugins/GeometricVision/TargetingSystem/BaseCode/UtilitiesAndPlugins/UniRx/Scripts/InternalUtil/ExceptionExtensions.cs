using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
	internal static class ExceptionExtensions
	{
		public static void Throw(this Exception exception)
		{
#if (NET_4_6 || NET_STANDARD_2_0)
			global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
#endif
            throw exception;
		}
	}
}
