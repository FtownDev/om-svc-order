﻿namespace om_svc_order.Services
{
    public interface ICacheService
    {
        T? GetData<T>(string key);

        void SetData<T>(string key, T data, int timeoutLength = 1);

        void InvalidateKeys(List<string> keysToDelete);
    }
}
