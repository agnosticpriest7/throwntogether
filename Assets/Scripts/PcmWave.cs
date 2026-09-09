using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ThrownTogether
{
    // Original PCM16 WAV samples bypass browser-dependent compressed audio decoding.
    public static class PcmWave
    {
        public static float[] Decode(byte[] bytes,out int channels,out int frequency)
        {
            channels=0; frequency=0;
            using var stream=new MemoryStream(bytes);
            using var reader=new BinaryReader(stream,Encoding.ASCII);
            if(new string(reader.ReadChars(4))!="RIFF") throw new InvalidDataException("Expected RIFF");
            reader.ReadUInt32();
            if(new string(reader.ReadChars(4))!="WAVE") throw new InvalidDataException("Expected WAVE");
            byte[] samples=null;
            while(stream.Position+8<=stream.Length)
            {
                var tag=new string(reader.ReadChars(4)); int size=checked((int)reader.ReadUInt32());
                long end=stream.Position+size;
                if(end>stream.Length) throw new InvalidDataException("Truncated WAV chunk");
                if(tag=="fmt ")
                {
                    if(size<16 || reader.ReadUInt16()!=1) throw new InvalidDataException("Expected PCM");
                    channels=reader.ReadUInt16(); frequency=reader.ReadInt32();
                    reader.ReadInt32(); reader.ReadUInt16();
                    if(reader.ReadUInt16()!=16) throw new InvalidDataException("Expected 16-bit PCM");
                }
                else if(tag=="data") samples=reader.ReadBytes(size);
                stream.Position=end+(size&1);
            }
            if(channels<1 || channels>2 || frequency<8000 || frequency>192000 || samples==null || samples.Length==0 || samples.Length%(channels*2)!=0)
                throw new InvalidDataException("Invalid PCM sample layout");
            var result=new float[samples.Length/2];
            for(int i=0;i<result.Length;i++) result[i]=(short)(samples[i*2]|samples[i*2+1]<<8)/32768f;
            return result;
        }
        public static AudioClip CreateClip(byte[] bytes,string name)
        {
            var samples=Decode(bytes,out int channels,out int frequency);
            var clip=AudioClip.Create(name,samples.Length/channels,channels,frequency,false);
            if(!clip.SetData(samples,0)) { UnityEngine.Object.Destroy(clip); throw new InvalidDataException("Could not set PCM audio samples"); }
            return clip;
        }
    }
}
