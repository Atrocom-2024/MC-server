using System.Net.Sockets;
using ProtoBuf;

namespace MC_server.GameRoom.Utils
{
    public static class ProtobufUtils
    {
        // 클래스의 인스턴스 데이터나 필드를 참조하지 않기 때문에, 해당 메서드는 인스턴스 메서드일 필요가 없음
        // 정적 메서드는 인스턴스 메서드보다 메모리를 덜 사용

        /// <summary>
        /// 객체를 Protobuf 직렬화하여 바이트 배열로 변환
        /// </summary>
        /// <typeparam name="Task"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeProtobuf<Task>(Task obj)
        {
            using var memoryStream = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(memoryStream, obj, PrefixStyle.Base128);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 네트워크 스트림에서 Protobuf 데이터를 역직렬화하여 객체로 변환
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        public static T DeserializeProtobuf<T>(NetworkStream networkStream)
        {
            return Serializer.DeserializeWithLengthPrefix<T>(networkStream, PrefixStyle.Base128);
        }
    }
}
