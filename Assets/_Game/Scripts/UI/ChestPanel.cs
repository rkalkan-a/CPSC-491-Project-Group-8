using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    // the popup you get when opening a chest. escape closes it. items will go in the slots later
    public class ChestPanel : MonoBehaviour
    {
        public static ChestPanel Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;

        private PlayerController player;
        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(Loot.Chest chest, PlayerController opener)
        {
            player = opener;
            if (player != null) player.InputEnabled = false;
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (player != null) player.InputEnabled = true;
            player = null;
        }

        private void Update()
        {
            if (!IsShowing) return;
            bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool back = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (esc || back) Hide();
        }
    }
}
