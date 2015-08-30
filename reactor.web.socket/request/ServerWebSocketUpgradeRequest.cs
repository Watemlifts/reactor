﻿///*--------------------------------------------------------------------------

//Reactor.Web.Sockets

//The MIT License (MIT)

//Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

//---------------------------------------------------------------------------*/

//using Reactor.Net;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Reactor.Web.Socket
//{
//    internal class ServerWebSocketUpgradeRequest
//    {
//        public Reactor.Http.Context Context  { get; set; }

//        public string SecWebSocketExtensions { get; set; }

//        public string SecWebSocketKey        { get; set; }

//        public string SecWebSocketVersion    { get; set; }

//        private ServerWebSocketUpgradeRequest(Reactor.Http.Context context)
//        {
//            this.Context                = context;

//            this.SecWebSocketExtensions = context.Request.Headers["Sec-WebSocket-Extensions"];

//            this.SecWebSocketKey        = context.Request.Headers["Sec-WebSocket-Key"];

//            this.SecWebSocketVersion    = context.Request.Headers["Sec-WebSocket-Version"];
//        }

//        #region Statics

//        public static ServerWebSocketUpgradeRequest Create(Reactor.Http.Context context)
//        {
//            try
//            {
//                var path       = context.Request.Url.AbsolutePath;

//                var method     = context.Request.Method;

//                var protocol   = context.Request.ProtocolVersion;

//                var upgrade    = context.Request.Headers["Upgrade"];

//                var connection = context.Request.Headers["Connection"];

//                if (protocol == HttpVersion.Version11 && method.ToLower() == "get" && upgrade.ToLower().Contains("websocket") && connection.ToLower().Contains("upgrade"))
//                {
//                    return new ServerWebSocketUpgradeRequest(context);
//                }

//                return null;
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        #endregion
//    }
//}
