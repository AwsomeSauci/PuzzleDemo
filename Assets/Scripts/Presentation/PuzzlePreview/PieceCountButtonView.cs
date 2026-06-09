using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PieceCountButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Graphic background;
        [SerializeField] private Color selectedColor = new Color(0.15f, 0.42f, 0.72f);
        [SerializeField] private Color regularColor = new Color(0.62f, 0.65f, 0.69f);
        [SerializeField] private Color selectedLabelColor = Color.white;
        [SerializeField] private Color regularLabelColor = Color.white;

        private int pieceCount;
        private Action<int> click;

        public void Bind(int pieceCount, string labelText, bool selected, Action<int> click)
        {
            this.pieceCount = pieceCount;
            this.click = click;
            label.text = labelText;

            if (background != null)
            {
                background.color = selected ? selectedColor : regularColor;
            }

            label.color = selected ? selectedLabelColor : regularLabelColor;
        }

        public void Unbind()
        {
            click = null;
            label.text = string.Empty;
        }

        public void OnClicked()
        {
            click?.Invoke(pieceCount);
        }
    }
}

