﻿/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using Reactor.Async;
using System;
using System.Net;
using System.Net.Sockets;

namespace Reactor.Tcp {

    /// <summary>
    /// Reactor TCP socket.
    /// </summary>
    public class Socket : Reactor.IDuplexable, IDisposable {

        #region States

        /// <summary>
        /// Readable state.
        /// </summary>
        internal enum State {
            /// <summary>
            /// A state indicating a paused state.
            /// </summary>
            Paused,
            /// <summary>
            /// A state indicating a reading state.
            /// </summary>
            Reading,
            /// <summary>
            /// A state indicating a resume state.
            /// </summary>
            Resumed,
            /// <summary>
            /// A state indicating a ended state.
            /// </summary>
            Ended
        }

        /// <summary>
        /// Readable mode.
        /// </summary>
        internal enum Mode {
            /// <summary>
            /// Flowing read semantics.
            /// </summary>
            Flowing,
            /// <summary>
            /// Non flowing read semantics.
            /// </summary>
            NonFlowing
        }

        #endregion

        private System.Net.Sockets.Socket             socket;
        private Reactor.Async.Spool                   spool;
        private Reactor.Async.Event                   onconnect;
        private Reactor.Async.Event                   ondrain;
        private Reactor.Async.Event                   onreadable;
        private Reactor.Async.Event<Reactor.Buffer>   ondata;
        private Reactor.Async.Event<Exception>        onerror;
        private Reactor.Async.Event                   onend;
        private Reactor.Streams.Reader                reader;
        private Reactor.Streams.Writer                writer;
        private Reactor.Buffer                        buffer;
        private Reactor.Interval                      poll;
        private State                                 state;
        private Mode                                  mode;
        
        #region Constructors

        /// <summary>
        /// Binds a new socket.
        /// </summary>
        /// <param name="socket">The socket to bind.</param>
        internal Socket (System.Net.Sockets.Socket socket) {
            this.spool      = Reactor.Async.Spool.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.ondata     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Paused;
            this.mode       = Mode.NonFlowing;
            this.socket     = socket;
            var stream      = new NetworkStream(socket);
            this.reader     = Reactor.Streams.Reader.Create(stream);
            this.writer     = Reactor.Streams.Writer.Create(stream);
            this.poll       = Reactor.Interval.Create(this._Poll, 1000);
            this.buffer     = new Reactor.Buffer();
            this.reader.OnRead  (this._Data);
            this.reader.OnError (this._Error);
            this.reader.OnEnd   (this._End);
            this.writer.OnDrain (this._Drain);
            this.writer.OnError (this._Error);
            this.writer.OnEnd   (this._End);
            this.onconnect.Emit();
            this.spool.Resume();
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public Socket (System.Net.IPAddress endpoint, int port) {
            this.spool      = Reactor.Async.Spool.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.ondata     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Paused;
            this.mode       = Mode.NonFlowing;
            this.spool.Pause();
            this.Connect(endpoint, port).Then(socket => {
                this.socket = socket;
                var stream  = new NetworkStream(socket);
                this.reader = Reactor.Streams.Reader.Create(stream);
                this.writer = Reactor.Streams.Writer.Create(stream);
                this.poll   = Reactor.Interval.Create(this._Poll, 1000);
                this.buffer = new Reactor.Buffer();
                this.reader.OnRead  (this._Data);
                this.reader.OnError (this._Error);
                this.reader.OnEnd   (this._End);
                this.writer.OnDrain (this._Drain);
                this.writer.OnError (this._Error);
                this.writer.OnEnd   (this._End);
                this.onconnect.Emit();
                this.spool.Resume();
            }).Error(this._Error);
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public Socket (string hostname, int port) {
            this.spool      = Reactor.Async.Spool.Create(1);
            this.onconnect  = Reactor.Async.Event.Create();
            this.ondrain    = Reactor.Async.Event.Create();
            this.onreadable = Reactor.Async.Event.Create();
            this.ondata     = Reactor.Async.Event.Create<Reactor.Buffer>();
            this.onerror    = Reactor.Async.Event.Create<Exception>();
            this.onend      = Reactor.Async.Event.Create();
            this.state      = State.Paused;
            this.mode       = Mode.NonFlowing;
            this.spool.Pause();
            this.ResolveHost(hostname).Then(endpoint => {
                this.Connect(endpoint, port).Then(socket => {
                    this.socket = socket;
                    var stream  = new NetworkStream(socket);
                    this.reader = Reactor.Streams.Reader.Create(stream);
                    this.writer = Reactor.Streams.Writer.Create(stream);
                    this.poll   = Reactor.Interval.Create(this._Poll, 1000);
                    this.buffer = new Reactor.Buffer();
                    this.reader.OnRead  (this._Data);
                    this.reader.OnError (this._Error);
                    this.reader.OnEnd   (this._End);
                    this.writer.OnDrain (this._Drain);
                    this.writer.OnError (this._Error);
                    this.writer.OnEnd   (this._End);
                    this.onconnect.Emit();
                    this.spool.Resume();
                }).Error(this._Error);
            }).Error(this._Error);
        }

        #endregion

        #region Events

        /// <summary>
        /// Subscribes this action to the OnConnect event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnConnect (Reactor.Action callback) {
            this.onconnect.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnConnect event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveConnect (Reactor.Action callback) {
            this.onconnect.Remove(callback);
        }

        /// <summary>
        /// Subscribes to the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnDrain(Reactor.Action callback) {
            this.ondrain.On(callback);
        }

        /// <summary>
        /// Unsubscribes from the OnDrain event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveDrain(Reactor.Action callback) {
            this.ondrain.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnReadable event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnReadable(Reactor.Action callback) {
            this.spool.Run(next => {
                this.mode = Mode.NonFlowing;
                this.onreadable.On(callback);
                this.Resume();
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the OnReadable event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveReadable(Reactor.Action callback) {
            this.spool.Run(next => {
                this.onreadable.Remove(callback);
                next();
            });
        }

        /// <summary>
        /// Subscribes this action to the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnRead (Reactor.Action<Reactor.Buffer> callback) {
            this.spool.Run(next => {
                this.mode = Mode.Flowing;
                this.ondata.On(callback);
                this.Resume();
                next();
            });
        }

        /// <summary>
        /// Unsubscribes this action from the OnRead event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveRead (Reactor.Action<Reactor.Buffer> callback) {
            this.spool.Run(next => {
                this.ondata.Remove(callback);
                next();
            });
        }

        /// <summary>
        /// Subscribes this action to the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnError (Reactor.Action<Exception> callback) {
            this.onerror.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnError event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveError (Reactor.Action<Exception> callback) {
            this.onerror.Remove(callback);
        }

        /// <summary>
        /// Subscribes this action to the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void OnEnd (Reactor.Action callback) {
            this.onend.On(callback);
        }

        /// <summary>
        /// Unsubscribes this action from the OnEnd event.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveEnd (Reactor.Action callback) {
            this.onend.Remove(callback);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads bytes from this streams buffer.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns></returns>
        public Reactor.Buffer Read(int count) {
            var subset = this.buffer.Read(count);
            if (this.buffer.Length == 0) {
                this.Resume();
            }
            return Reactor.Buffer.Create(subset);            
        }

        /// <summary>
        /// Reads bytes from this streams buffer.
        /// </summary>
        /// <returns></returns>
        public Reactor.Buffer Read() {
            return this.Read(this.buffer.Length);
        }

        /// <summary>
        /// Unshifts this buffer back to this stream.
        /// </summary>
        /// <param name="buffer">The buffer to unshift.</param>
        public void Unshift(Reactor.Buffer buffer) {
            this.buffer.Unshift(buffer);
        }

        /// <summary>
        /// Writes this buffer to the stream.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="callback">A callback to signal when this buffer has been written.</param>
        public Reactor.Async.Future Write(Reactor.Buffer buffer) {
            return new Reactor.Async.Future((resolve, reject)=>{
                this.spool.Run(next => {
                    this.writer.Write(buffer)
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Flushes this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this buffer has been flushed.</param>
        public Reactor.Async.Future Flush() {
            return new Reactor.Async.Future((resolve, reject)=>{
                this.spool.Run(next => {
                    this.writer.Flush()
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Ends this stream.
        /// </summary>
        /// <param name="callback">A callback to signal when this stream has ended.</param>
        public Reactor.Async.Future End() {
            return new Reactor.Async.Future((resolve, reject)=>{
                this.spool.Run(next => {
                    this.writer.End()
                               .Then(resolve)
                               .Error(reject)
                               .Finally(next);
                });
            });
        }

        /// <summary>
        /// Pauses this stream.
        /// </summary>
        public void Pause() {
            this.state = State.Paused;
        }

        /// <summary>
        /// Resumes this stream.
        /// </summary>
        public void Resume() {
            this.spool.Run(next => {
                this.state = State.Resumed;
                this._Read();
                next();
            });
        }

        /// <summary>
        /// Pipes data to a writable stream.
        /// </summary>
        /// <param name="writable"></param>
        /// <returns></returns>
        public Reactor.IReadable Pipe (Reactor.IWritable writable) {
            this.OnRead(data => {
                this.Pause();
                writable.Write(data)
                        .Then(this.Resume)
                        .Error(this._Error);
            });
            this.OnEnd (() => writable.End());
            return this;
        }

        #endregion

        #region Socket

        /// <summary>
        /// Gets the address family of the Socket.
        /// </summary>
        public AddressFamily AddressFamily {
            get { return this.socket.AddressFamily; }
        }

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        public int Available {
            get { return this.socket.Available; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the Socket is in blocking mode.
        /// </summary>
        public bool Blocking {
            get { return this.socket.Blocking; }
            set { this.socket.Blocking = value; }
        }

        /// <summary>
        /// Gets a value that indicates whether a Socket is connected to a remote host as of the last Send or Receive operation.
        /// </summary>
        public bool Connected {
            get { return this.socket.Connected; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows Internet Protocol (IP) datagrams to be fragmented.
        /// </summary>
        public bool DontFragment {
            get { return this.socket.DontFragment; }
            set { this.socket.DontFragment = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket can send or receive broadcast packets.
        /// </summary>
        public bool EnableBroadcast {
            get { return this.socket.EnableBroadcast; }
            set { this.socket.EnableBroadcast = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows only one process to bind to a port.
        /// </summary>
        public bool ExclusiveAddressUse {
            get { return this.socket.ExclusiveAddressUse; }
            set { this.socket.ExclusiveAddressUse = value; }
        }

        /// <summary>
        /// Gets the operating system handle for the Socket.
        /// </summary>
        public IntPtr Handle {
            get { return this.socket.Handle; }
        }

        /// <summary>
        /// Gets a value that indicates whether the Socket is bound to a specific local port.
        /// </summary>
        public bool IsBound {
            get { return this.socket.IsBound; }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether the Socket will delay closing a socket in an attempt to send all pending data.
        /// </summary>
        public LingerOption LingerState {
            get { return this.socket.LingerState; }
            set { this.socket.LingerState = value; }
        }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint {
            get { return this.socket.LocalEndPoint; }
        }

        /// <summary>
        /// Gets or sets a value that specifies whether outgoing multicast packets are delivered to the sending application.
        /// </summary>
        public bool MulticastLoopback {
            get { return this.socket.MulticastLoopback; }
            set { this.socket.MulticastLoopback = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the stream Socket is using the Nagle algorithm.
        /// </summary>
        public bool NoDelay {
            get { return this.socket.NoDelay; }
            set { this.socket.NoDelay = value; }
        }

        /// <summary>
        /// Indicates whether the underlying operating system and network adaptors support Internet Protocol version 4 (IPv4).
        /// </summary>
        public static bool OSSupportsIPv4{
            get { return Socket.OSSupportsIPv4; }
        }

        /// <summary>
        /// Indicates whether the underlying operating system and network adaptors support Internet Protocol version 6 (IPv6).
        /// </summary>
        public static bool OSSupportsIPv6 {
            get { return Socket.OSSupportsIPv6; }
        }

        /// <summary>
        /// Gets the protocol type of the Socket.
        /// </summary>
        public ProtocolType ProtocolType {
            get { return this.socket.ProtocolType; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the receive buffer of the Socket.
        /// </summary>
        public int ReceiveBufferSize {
            get { return this.socket.ReceiveBufferSize; }
            set { this.socket.ReceiveBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous Receive call will time out.
        /// </summary>
        public int ReceiveTimeout {
            get { return this.socket.ReceiveTimeout; }
            set { this.socket.ReceiveTimeout = value; }
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint {
            get { return this.socket.RemoteEndPoint; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the send buffer of the Socket.
        /// </summary>
        public int SendBufferSize {
            get { return this.socket.SendBufferSize; }
            set { this.socket.SendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous Send call will time out.
        /// </summary>
        public int SendTimeout {
            get { return this.socket.SendTimeout; }
            set { this.socket.SendTimeout = value; }
        }

        /// <summary>
        /// Gets the type of the Socket.
        /// </summary>
        public SocketType SocketType {
            get { return this.socket.SocketType; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the Time To Live (TTL) value of Internet Protocol (IP) packets sent by the Socket.
        /// </summary>
        public short Ttl {
            get { return this.socket.Ttl; }
            set { this.socket.Ttl = value; }
        }

        /// <summary>
        /// Specifies whether the socket should only use Overlapped I/O mode.
        /// </summary>
        public bool UseOnlyOverlappedIO {
            get { return this.socket.Blocking; }
            set { this.socket.Blocking = value; }
        }

        /// <summary>
        /// Sets the specified Socket option to the specified Boolean value.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified value, represented as a byte array.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified integer value.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        /// <summary>
        /// Sets the specified Socket option to the specified value, represented as an object.
        /// </summary>
        /// <param name="optionLevel"></param>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue) {
            this.socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        #endregion

        #region Buffer

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer, int index, int count) {
            return this.Write(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte[] buffer) {
            return this.Write(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string data) {
            return this.Write(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (string format, params object[] args) {
            format = string.Format(format, args);
            return this.Write(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Writes this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (byte data) {
            return this.Write(new byte[1] { data });
        }

        /// <summary>
        /// Writes a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (bool value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (short value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ushort value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (int value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (uint value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (long value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (ulong value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (float value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A future resolved when this write has completed.</returns>
        public Reactor.Async.Future Write (double value) {
            return this.Write(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void Unshift (byte[] buffer, int index, int count) {
            this.Unshift(Reactor.Buffer.Create(buffer, 0, count));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="buffer"></param>
        public void Unshift (byte[] buffer) {
            this.Unshift(Reactor.Buffer.Create(buffer));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (char data) {
            this.Unshift(data.ToString());
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (string data) {
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Unshift (string format, params object[] args) {
            format = string.Format(format, args);
            this.Unshift(System.Text.Encoding.UTF8.GetBytes(format));
        }

        /// <summary>
        /// Unshifts this data to the stream.
        /// </summary>
        /// <param name="data"></param>
        public void Unshift (byte data) {
            this.Unshift(new byte[1] { data });
        }

        /// <summary>
        /// Unshifts a System.Boolean value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (bool value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (short value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt16 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ushort value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (int value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt32 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (uint value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Int64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (long value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.UInt64 value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (ulong value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Single value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (float value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Unshifts a System.Double value to the stream.
        /// </summary>
        /// <param name="value"></param>
        public void Unshift (double value) {
            this.Unshift(BitConverter.GetBytes(value));
        }

        #endregion

        #region Internal

        /// <summary>
        /// Resolves the host ip address from the hostname.
        /// </summary>
        /// <param name="hostname">The hostname or ip to resolve.</param>
        /// <returns></returns>
        private Reactor.Async.Future<System.Net.IPAddress> ResolveHost (string hostname) {
            return new Reactor.Async.Future<System.Net.IPAddress>((resolve, reject) => {
                Reactor.Dns.GetHostAddresses(hostname)
                           .Then(addresses => {
                                if (addresses.Length == 0) 
                                    reject(new Exception("host not found"));
                                else
                                    resolve(addresses[0]);
                            }).Error(reject);
            });
        }
        
        /// <summary>
        /// Connects to a remote TCP endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        private Reactor.Async.Future<System.Net.Sockets.Socket> Connect (System.Net.IPAddress endpoint, int port) {
            return new Reactor.Async.Future<System.Net.Sockets.Socket>((resolve, reject) => {
                var socket   = new System.Net.Sockets.Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try {
                    socket.BeginConnect(endpoint, port, result => {
                        Loop.Post(() => {
                            try {
                                socket.EndConnect(result);
                                resolve(socket);
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch(Exception error) {
                    reject(error);
                }
            });
        }

        #endregion

        #region Machine

        /// <summary>
        /// Polls the active state on this socket.
        /// </summary>
        private int poll_failed = 0;
        private void _Poll () {
            /* poll within a fiber to prevent interuptions
             * from the main thread. allow for 4 failed
             * attempts before signalling termination. */
            Reactor.Fibers.Fiber.Create(() => {
                var result = !(this.socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
                if (!result) {
                    poll_failed = poll_failed + 1;
                    if (poll_failed > 4) {
                        throw new Exception("socket: poll detected unexpected termination");
                    }
                } else poll_failed = 0;
            }).Error(this._Error);
        }

        /// <summary>
        /// Disconnects this socket.
        /// </summary>
        /// <returns></returns>
        private Reactor.Async.Future _Disconnect () {
            return new Reactor.Async.Future((resolve, reject) => {
                try {
                    this.socket.BeginDisconnect(false, (result) => {
                        Loop.Post(() => {
                            try {
                                socket.EndDisconnect(result);
                                resolve();
                            }
                            catch (Exception error) {
                                reject(error);
                            }
                        });
                    }, null);
                }
                catch(Exception error) {
                    reject(error);
                }
            });
        }

        /// <summary>
        /// Handles OnDrain events.
        /// </summary>
        private void _Drain () {
            this.ondrain.Emit();
        }

        /// <summary>
        /// Begins reading from the underlying stream.
        /// </summary>
        private void _Read () {
            if (this.state == State.Resumed) {
                this.state = State.Reading;
                this.reader.Read();
            }
        }

        /// <summary>
        /// Handles incoming data from the stream.
        /// </summary>
        /// <param name="buffer"></param>
        private void _Data (Reactor.Buffer buffer) {
            this.state = State.Resumed;
            switch (this.mode) {
                case Mode.Flowing:
                    this.ondata.Emit(buffer);
                    this._Read();
                    break;
                case Mode.NonFlowing:
                    this.buffer.Write(buffer);
                    this.onreadable.Emit();
                    break;
            } 
        }

        /// <summary>
        /// Handles stream errors.
        /// </summary>
        /// <param name="error"></param>
        private void _Error (Exception error) {
            if (this.state != State.Ended) { 
                this.onerror.Emit(error);
            }
        }

        /// <summary>
        /// Terminates the stream.
        /// </summary>
        public void _End    () {
            if (this.state != State.Ended) {
                this.state = State.Ended;
                try { this.socket.Shutdown(SocketShutdown.Send); } catch {}
                this._Disconnect();
                if (this.poll   != null) this.poll.Clear();
                if (this.writer != null) this.writer.Dispose();
                if (this.reader != null) this.reader.Dispose();
                this.spool.Dispose();
                this.onend.Emit();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of this stream.
        /// </summary>
        public void Dispose() {
            this._End();
        }

        #endregion

        #region Statics

        /// <summary>
        /// Creates a new socket. Connects to localhost on this port.
        /// </summary>
        /// <param name="port">The port to connect to.</param>
        /// <returns></returns>
        public static Socket Create (int port) {
            return new Socket("localhost", port);
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="hostname">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns></returns>
        public static Socket Create (string hostname, int port) {
            return new Socket(hostname, port);
        }

        /// <summary>
        /// Creates a new socket.
        /// </summary>
        /// <param name="hostname">The endpoint to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns></returns>
        public static Socket Create (IPAddress address, int port) {
            return new Socket(address, port);
        }

        #endregion
    }
}