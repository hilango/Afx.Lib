﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Afx.Sockets
{
    #region tcp client
    /// <summary>
    /// tcp socket 连接成功回调
    /// </summary>
    /// <param name="client">TcpSocket</param>
    /// <param name="isSuccess">isSuccess</param>
    public delegate void TcpAsyncConnectEvent(TcpSocketAsync client, bool isSuccess);

    /// <summary>
    /// tcp socket 接收数据回调
    /// </summary>
    /// <param name="client">TcpSocket</param>
    /// <param name="data">接收数据</param>
    /// <param name="length">接收数据长度</param>
    public delegate void TcpReceiveEvent(TcpSocketAsync client, byte[] data, int length);

    /// <summary>
    /// tcp socket 异常回调
    /// </summary>
    /// <param name="client">TcpSocket</param>
    /// <param name="ex">Exception</param>
    public delegate void TcpErrorEvent(TcpSocketAsync client, Exception ex);

    /// <summary>
    /// tcp soket 正在接收数据回调
    /// </summary>
    /// <param name="client">TcpSocketAsync</param>
    /// <param name="position">已读取数据position</param>
    /// <param name="length">数据总长度</param>
    public delegate void TcpReadingEvent(TcpSocketAsync client, int position, int length);
    #endregion

    #region tcp server
    /// <summary>
    /// tcp server  监听客户端连接回调
    /// </summary>
    /// <param name="client">TcpSocket</param>
    public delegate void TcpAcceptEvent(TcpSocketAsync client);

    /// <summary>
    /// tcp server 异常回调
    /// </summary>
    /// <param name="server">TcpServer</param>
    /// <param name="ex">Exception</param>
    public delegate void TcpServerErrorEvent(TcpServer server, Exception ex);
    #endregion

    #region udp server
    /// <summary>
    /// udp socket 接收数据回调
    /// </summary>
    /// <param name="remoteEP">发送端终结点</param>
    /// <param name="data">接收的数据</param>
    /// <param name="length">接收数据长度</param>
    public delegate void UdpReceiveEvent(EndPoint remoteEP, byte[] data, int length);

    /// <summary>
    /// udp socket 异常回调
    /// </summary>
    /// <param name="remoteEP">发送端终结点</param>
    /// <param name="ex">异常</param>
    public delegate void UdpServerErrorEvent(EndPoint remoteEP, Exception ex);
    #endregion
}
