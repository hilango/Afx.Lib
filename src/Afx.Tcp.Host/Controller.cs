﻿using System;
using System.Collections.Generic;
using System.Text;

using Afx.Tcp.Protocols;

namespace  Afx.Tcp.Host
{
    /// <summary>
    /// cmd Controller
    /// </summary>
    public abstract class Controller : IDisposable
    {
        /// <summary>
        /// Session
        /// </summary>
        public virtual Session Session { get; private set; }

        private MsgData msg;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="msg">介绍到的msg</param>
        public virtual void Init(Session session, MsgData msg)
        {
            this.IsDisposed = false;
            this.Session = session;
            this.msg = msg;
        }

        /// <summary>
        /// 获取接收到model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected virtual T GetData<T>()
        {
            return this.msg != null ? this.msg.GetData<T>() : default(T);
        }

        /// <summary>
        /// Result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="status"></param>
        /// <param name="data"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual ActionResult Result<T>(MsgStatus status, T data, string msg)
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Succeed, data, null);

            return result;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual ActionResult Success<T>(T data, string msg)
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Succeed, data, msg);

            return result;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual ActionResult Success<T>(T data)
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Succeed, data, null);

            return result;
        }

        /// <summary>
        /// Success
        /// </summary>
        /// <returns></returns>
        protected virtual ActionResult Success()
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Succeed);

            return result;
        }

        /// <summary>
        /// Error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        protected virtual ActionResult Error(string error)
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Error, error);

            return result;
        }

        /// <summary>
        /// Error
        /// </summary>
        /// <returns></returns>
        protected virtual ActionResult Error()
        {
            ActionResult result = new ActionResult();
            result.SetMsg(MsgStatus.Error);

            return result;
        }

        /// <summary>
        /// OnExecuting
        /// </summary>
        public virtual void OnExecuting()
        {

        }

        /// <summary>
        /// OnResult
        /// </summary>
        /// <param name="result"></param>
        public virtual void OnResult(ActionResult result)
        {

        }

        /// <summary>
        /// OnException
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnException(ExceptionContext context)
        {

            
        }

        /// <summary>
        /// IsDisposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            this.msg = null;
            this.Session = null;
            this.IsDisposed = true;
        }
    }
}
