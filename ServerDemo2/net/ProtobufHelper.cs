using System;
using System.Collections.Generic;
using System.IO;
using Messages.Type;

public struct PackageConstant
{
    public static int TypeOffset = 0;      // 消息类型，占1字节，偏移值0
    public static int LengthOffset = 1;    // 消息包长度，占2字节，偏移值1
    public static int HeadLength = 3;      // 包头长度(含消息类型和消息包长度)
}

class ProtobufHelper
{
    public static byte[] PackMessage<T>(T msg, ResponseType type)
    {
        byte packageType = (byte)type;
        byte[] packageBody = Serialize<T>(msg);
        int packageLength = PackageConstant.HeadLength + packageBody.Length;

        List<byte> packageHeadList = new List<byte>();
        packageHeadList.Add(packageType);
        packageHeadList.AddRange(BitConverter.GetBytes((short)packageLength));
        packageHeadList.AddRange(packageBody);

        return packageHeadList.ToArray();
    }

    public static byte[] Serialize<T>(T msg)
    {
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(ms, msg);
            bytes = new byte[ms.Position];
            var fullBytes = ms.GetBuffer();
            Array.Copy(fullBytes, bytes, bytes.Length);
        }
        return bytes;
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        using (Stream ms = new MemoryStream(bytes))
        {
            return ProtoBuf.Serializer.Deserialize<T>(ms);
        }
    }
}
