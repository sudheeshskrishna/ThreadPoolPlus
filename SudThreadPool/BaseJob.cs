﻿using System;

namespace Sud.Threading
{
    public class BaseJob<TParam> where TParam : IComparable
    {
        public BaseJob(BaseWorkItem<TParam> WorkItem, TParam param)
        {
            this.WorkItem = WorkItem;
            //this.promise = new Promise<TReturn>();
            this.param = param;
            //PromiseHandler = new PromiseHandler<TReturn>(this.promise);
        }

        public BaseWorkItem<TParam> WorkItem { get; set; }
        //public PromiseHandler<TReturn> PromiseHandler { get; set; }
        //public Promise<TReturn> promise { get; set; }
        public TParam param { get; set; }

        //public void Resolve(TReturn ob)
        //{
        //    if (promise != null)
        //        promise.Resolve(ob);
        //}

        //public void Reject(Exception ob)
        //{
        //    if (promise != null)
        //        promise.Reject(ob);
        //}
    }

    //public class PromiseHandler<TReturn> where TReturn : IComparable
    //{
    //    public PromiseHandler(Promise<TReturn> Promise)
    //    {
    //        this.promise = Promise;
    //    }

    //    private Promise<TReturn> promise;



    //    public void then(PromiseCallBack<TReturn> successCB, PromiseCallBack<Exception> errorCB)
    //    {
    //        if (promise != null)
    //        {
    //            promise.OnSuccess(successCB);
    //            promise.OnError(errorCB);
    //        }
    //    }
    //}
}
