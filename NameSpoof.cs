using Photon.Pun;
using Steamworks;

namespace TestUnityPlugin
{
    internal class NameSpoof : IVariableCheat<string>
    {
        public static string _value = SteamFriends.GetPersonaName();
        public static bool _enabled = true;

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_enabled)
                {
                    PhotonNetwork.LocalPlayer.NickName = _value;
                }
            }
        }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled)
                {
                    PhotonNetwork.LocalPlayer.NickName = _value;
                }
                else
                {
                    PhotonNetwork.LocalPlayer.NickName = SteamFriends.GetPersonaName();
                }
            }
        }

        public static void TrySetNickname()
        {
            if (_enabled)
            {
                PhotonNetwork.LocalPlayer.NickName = _value;
            }
        }

        public static void OnEnable()
        {
            PhotonNetwork.LocalPlayer.NickName = _value;
        }

        public static void OnDisable()
        {
            PhotonNetwork.LocalPlayer.NickName = SteamFriends.GetPersonaName();
        }

        public static void OnValueChanged(string newValue)
        {
            _value = newValue;
            PhotonNetwork.LocalPlayer.NickName = newValue;
        }
    }
}