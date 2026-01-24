namespace SurviveTheHuntClient.Plugins.Xmas.Models
{
    internal class PrezzieState
    {
        private PrezzieLocation _location;
        internal PrezzieLocation Location { get => _location; }
        private byte[] _rgba;
        internal byte[] Rgba => new byte[4] { _rgba[0], _rgba[1], _rgba[2], _rgba[3] };

        internal bool HasPlaced = false;

        internal PrezzieState(PrezzieLocation location, byte[] rgbaColour = null)
        {
            _location = location;
            
            if (rgbaColour == null)
            {
                rgbaColour = new byte[4] { 255, 255, 255, 255};
            }
            byte[] rgba;
            if(rgbaColour.Length <= 4)
            {
                rgba = new byte[4] { 0, 0, 0, 255 };
                rgbaColour.CopyTo(rgba, 0);
            }
            else
            {
                rgba = new byte[4] { rgbaColour[0], rgbaColour[1], rgbaColour[2], rgbaColour[3] };
                
            }
            _rgba = rgba;
        }
    }
}
