﻿using System;
using System.Collections.Generic;

namespace AccessBattle.Networking
{
    /// <summary>
    /// Packet Format: [STX|LEN|TYPE|DATA|CHSUM|ETX]
    /// See GameServer class.
    /// </summary>
    /// <remarks>
    /// STX and ETX are used to mark the start and end of a packet
    /// They shoud be escaped if they apear in the data within:
    /// STX = 0x02; ETX = 0x03
    /// We are using the same method that is used in XBee modules with API mode 2:
    /// 0x7D is the escape character. The next character after 0x7D will be
    /// the escaped character, which must be XOR'ed with 0x20.
    /// This means, if a 0x02 appears in the data, we XOR it with 0x20 and add an 0x7D before:
    /// 0x02 => 0x7D 0x22
    /// 0x03 => 0x7D 0x23
    /// 0x7D => 0x7D 0x5D
    /// This conversion takes place AFTER the packet bytes have been put together, just before transmitting.
    /// The first and last byte have to be ignored of course!
    /// </remarks>
    public class NetworkPacket
    {
        /// <summary>Packet data.</summary>
        public byte[] Data { get; private set; }
        /// <summary>Packet data type.</summary>
        public byte PacketType { get; private set; }

        /// <summary>Start byte.</summary>
        public const byte STX = 0x02;
        /// <summary>End byte.</summary>
        public const byte ETX = 0x03;
        /// <summary>Escape byte.</summary>
        public const byte ESC = 0x7D;
        /// <summary>Mask byte.</summary>
        public const byte MASK = 0x20;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Packet data.</param>
        /// <param name="packetType">Packet type.</param>
        public NetworkPacket(byte[] data, byte packetType)
        {
            Data = data;
            PacketType = packetType;
        }

        /// <summary>
        /// Create packet from received bytes.
        /// Use this for buffers that contain exaclty one packet.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static NetworkPacket FromByteArray(byte[] buffer)
        {
            int stx, etx;
            return FromByteArray(buffer, out stx, out etx);
        }

        /// <summary>
        /// Create packet from received bytes.
        /// Use this for buffers that contain multiple packets.
        /// The stxIndex and etxIndex will tell you where the extracted packet was located within the provided buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stxIndex">Start of the packet within the provided byte array.</param>
        /// <param name="etxIndex">End of the packet within the provided byte array.</param>
        /// <returns></returns>
        public static NetworkPacket FromByteArray(byte[] buffer, out int stxIndex, out int etxIndex)
        {
            etxIndex = -1;
            stxIndex = -1;
            NetworkPacket packet = null;
            if (buffer == null) return null;

            // Do a sweep and unescape the packet
            var rawByteList = new List<byte>(buffer.Length);

            for (int i = 0; i < buffer.Length; ++i)
            {
                if (stxIndex < 0)
                {
                    if (buffer[i] == STX)
                    {
                        stxIndex = i;
                        rawByteList.Add(buffer[i]);
                    }
                    continue;
                }
                if (buffer[i] == ETX)
                {
                    etxIndex = i;
                    rawByteList.Add(buffer[i]);
                    break;
                }
                if (buffer[i] == ESC)
                    rawByteList.Add((byte)(buffer[++i] ^ MASK));
                else
                    rawByteList.Add(buffer[i]);
            }
            var rawBytes = rawByteList.ToArray();

            // Check if packet is valid:
            // Incomplete
            if (stxIndex == -1 || etxIndex == -1) return null;
            // Too short
            if (rawBytes.Length < 6) return null;
            // Length check
            var length = rawBytes[1] << 8 | rawBytes[2];
            if (length != rawBytes.Length - 6) return null;
            // Checksum check
            var chsumIndex = rawBytes.Length - 2;
            var sum = 0;
            for (int i = 2; i < chsumIndex; ++i)
                sum ^= rawBytes[i];
            if (sum != rawBytes[chsumIndex]) return null;

            var type = rawBytes[3];
            var data = new byte[length];
            Array.Copy(rawBytes, 4, data, 0, length);

            packet = new NetworkPacket(data, type);

            return packet;
        }

        /// <summary>
        /// Converts this packet to a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            var data = Data;
            if (data == null) return null;

            // Length: Constant part (STX,LEN,TYPE,CHSUM,ETX) = 6
            //         + length of data
            var rawBytes = new byte[6 + data.Length];
            var chsumIndex = rawBytes.Length - 2;

            rawBytes[0] = STX;
            rawBytes[1] = (byte)((data.Length >> 8) & 0xFF);
            rawBytes[2] = (byte)(data.Length & 0xFF);
            rawBytes[3] = PacketType;
            Array.Copy(data, 0, rawBytes, 4, data.Length);
            // Checksum
            var sum = 0;
            for (int i = 2; i < chsumIndex; ++i)
                sum ^= rawBytes[i];
            rawBytes[chsumIndex] = (byte)sum;
            rawBytes[chsumIndex + 1] = ETX;

            // Now do the escaping:
            // Adding 10 preemptive bytes to capacity for the escaping
            var finalBytes = new List<byte>(rawBytes.Length + 10);

            finalBytes.Add(rawBytes[0]);
            for (int i = 1; i < rawBytes.Length - 1; ++i)
            {
                var b = rawBytes[i];
                if (b == STX || b == ETX || b == ESC)
                {
                    finalBytes.Add(ESC);
                    finalBytes.Add((byte)(b ^ MASK));
                }
                else finalBytes.Add(b);
            }
            finalBytes.Add(rawBytes[rawBytes.Length - 1]);
            return finalBytes.ToArray();
        }
    }
}
