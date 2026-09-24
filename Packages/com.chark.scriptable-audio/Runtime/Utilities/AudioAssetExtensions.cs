using System;

namespace CHARK.ScriptableAudio.Utilities
{
    internal static class AudioAssetExtensions
    {
        public static TData GetData<TData>(this AudioEventAsset asset) where TData : IAudioEventData
        {
            if (asset.TryGetData<TData>(out var data))
            {
                return data;
            }

            throw new Exception(
                $"Could not retrieve Event data of type {typeof(TData)} from {nameof(asset)}"
            );
        }

        public static TData GetData<TData>(this AudioParameterAsset asset) where TData : IAudioParameterData
        {
            if (asset.TryGetData<TData>(out var data))
            {
                return data;
            }

            throw new Exception(
                $"Could not retrieve Parameter data of type {typeof(TData)} from {nameof(asset)}"
            );
        }

        public static bool TryGetData<TData>(this AudioEventAsset asset, out TData resultData)
            where TData : IAudioEventData
        {
            resultData = default;

            if (asset == false)
            {
                return false;
            }

            var data = asset.Data;
            if (data == null || data.IsValid == false)
            {
                return false;
            }

            if (data is TData typedData)
            {
                resultData = typedData;
                return true;
            }

            return false;
        }

        public static bool TryGetData<TData>(this AudioParameterAsset asset, out TData resultData)
            where TData : IAudioParameterData
        {
            resultData = default;

            if (asset == false)
            {
                return false;
            }

            var data = asset.Data;
            if (data == null || data.IsValid == false)
            {
                return false;
            }

            if (data is TData typedData)
            {
                resultData = typedData;
                return true;
            }

            return false;
        }
    }
}
