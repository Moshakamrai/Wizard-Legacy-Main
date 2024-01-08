using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public interface ICancelable : IDisposable
    {
        bool IsDisposed { get; }
    }
}
