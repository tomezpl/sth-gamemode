using SurviveTheHuntShared.Models;
using System;

namespace SurviveTheHuntShared.Interfaces
{
    public interface IPluginState
    {
        object Get(byte key);
        T Get<T>(byte key);
        TRet Get<TRet, TEnum>(TEnum key) where TEnum : IComparable;

        void Set<TEnum>(TEnum key, object value, PluginContextBase pluginContext = null) where TEnum : IComparable;
    }
}
