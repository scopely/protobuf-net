using Xunit;
using System;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{

    public class UnknownTypes
    {
        public abstract class Base
        {
            public int Number { get; set; }
        }

        public class Derived : Base
        {
            public string Foo { get; set; }
        }

        public class OtherDerived : Base
        {
            public string Bar { get; set; }
        }

        public class SuperDerived : Derived
        {
            public string Baz { get; set; }
        }

        public class Unknown : Base
        {

        }

        [Fact]
        public void ReadTypeWithUnknownSubtype()
        {
            var model = TypeModel.Create();

            model.Add(typeof(Base), false).Add("Number")
                .UnknownConstructType = typeof(Unknown);
            model.Add(typeof(Derived), false).Add("Foo");
            model[typeof(Base)].AddSubType(5, typeof(Derived));
            model.Add(typeof(OtherDerived), false).Add("Bar");
            model[typeof(Base)].AddSubType(10, typeof(OtherDerived));

            var orig = new Derived();
            orig.Number = 50;
            orig.Foo = "bar";

            var serialized = Serialize(model, orig);
            serialized[0] = 50; // Changing sub-type field number to 6

            var deserialized = (Base)Deserialize<Base>(model, serialized);
            Assert.IsType<Unknown>(deserialized);
            Assert.Equal(50, deserialized.Number);


            model.CompileInPlace();
            serialized = Serialize(model, orig);
            serialized[0] = 50;

            deserialized = (Base)Deserialize<Base>(model, serialized);
            Assert.IsType<Unknown>(deserialized);
            Assert.Equal(50, deserialized.Number);

            var compiled = model.Compile();
            serialized = Serialize(compiled, orig);
            serialized[0] = 50; 

            deserialized = (Base)Deserialize<Base>(compiled, serialized);
            Assert.IsType<Unknown>(deserialized);
            Assert.Equal(50, deserialized.Number);
        }

        byte[] Serialize(TypeModel model, object value)
        {
            Type type = value.GetType();
            int key = model.GetKey(ref type);

            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = ProtoWriter.Create(ms, model, null))
                {
                    writer.SetRootObject(value);
                    model.Serialize(key, value, writer);
                    writer.Close();

                    return ms.ToArray();
                }
            }
        }

        object Deserialize<T>(TypeModel model, byte[] data)
        {
            Type type = typeof(T);
            int key = model.GetKey(ref type);

            using (MemoryStream ms = new MemoryStream(data))
            {
                ProtoReader reader = null;
                try
                {
                    reader = ProtoReader.Create(ms, model, null, ProtoReader.TO_EOF);
                    return model.Deserialize(key, null, reader);
                }
                finally
                {
                    ProtoReader.Recycle(reader);
                }
            }
        }
    }

}
