﻿/*--------------------------------------------------------------------------

Reactor.Fusion

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

namespace Reactor.Fusion.Protocol {

    public interface IPacketSerializer {

        /// <summary>
        /// Serializers this packet to bytes.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <returns>The serialized packet or null if error.</returns>
        byte[] Serialize(Packet packet);

        /// <summary>
        /// Deserialized these bytes into a packet.
        /// </summary>
        /// <param name="bytes">The bytes to deserialize.</param>
        /// <param name="packetType">the Output packetType</param>
        /// <returns>A Packet on success or null if error.</returns>
        Packet Deserialize(byte[] bytes, out PacketType packetType);
    }
}
