using NorvinskMaps.Utils;
using EFT;
using UnityEngine;

namespace NorvinskMaps.UI.Controls
{
    public class PlayerPositionText : AbstractTextControl
    {
        public static PlayerPositionText Create(GameObject parent, float fontSize)
        {
            var text = Create<PlayerPositionText>(parent, "PlayerPositionText", fontSize);
            return text;
        }

        private void Update()
        {
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
            Text.text = $"{Localization.T("Player")}: {mapPosition.x:F} {mapPosition.y:F} {mapPosition.z:F}";
        }
    }
}
