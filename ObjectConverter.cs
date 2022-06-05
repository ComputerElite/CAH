using System.Reflection;

namespace Cards_against_humanity
{
    public class ObjectConverterIgnore : System.Attribute
    {

    }
    public class ObjectConverter
    {
        public static NewMom Convert<NewMom, YourMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static NewMom ConvertCopy<NewMom, YourMom>(YourMom toConvert) where NewMom : new()
        {
            if (toConvert == null) return default(NewMom);
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null) typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static NewMom ConvertCopy<NewMom, YourMom, HisMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null && Attribute.IsDefined(typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), typeof(ObjectConverterIgnore))) typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).SetValue(converted, father.GetValue(toConvert));
            }
            foreach (FieldInfo father in typeof(HisMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static NewMom Convert<NewMom, YourMom, HisMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            foreach (FieldInfo father in typeof(HisMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }
    }
}