using System;

namespace Sud.Threading
{
    public class Promise<TReturn> where TReturn : IComparable
    {
        private PromiseCallBack<TReturn> successCallback;
        private PromiseCallBack<Exception> errorCallback;
        public void OnSuccess(PromiseCallBack<TReturn> cb)
        {
            successCallback = cb;
        }

        public void OnError(PromiseCallBack<Exception> cb)
        {
            errorCallback = cb;
        }

        public void then(PromiseCallBack<TReturn> successCB, PromiseCallBack<Exception> errorCB)
        {
            successCallback = successCB;
            errorCallback = errorCB;
        }

        internal void Resolve(TReturn ob)
        {
            //if (successCallback != null)
            //    successCallback(ob);            
            successCallback?.Invoke(ob);
            //return default(TReturn);
        }

        internal void Reject(Exception ob)
        {
            //if (errorCallback != null)
            //    errorCallback(ob);
            errorCallback?.Invoke(ob);
            //return ob;

        }
    }

    //public delegate void PromiseCallBack();
    public delegate void PromiseCallBack<TReturn>(TReturn ob);
    public delegate TReturn PoolWorkItem<TParam, TReturn>(TParam ob);
    public delegate void BaseWorkItem<TParam>(TParam ob);
}
