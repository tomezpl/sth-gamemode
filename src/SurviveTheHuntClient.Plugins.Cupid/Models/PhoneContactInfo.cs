namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal struct PhoneContactInfo
    {
        internal readonly string Name;
        internal readonly string TextureName;

        internal PhoneContactInfo(string name, string textureName)
        {
            Name = name;
            TextureName = textureName;
        }
    }
}
