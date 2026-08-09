using UnityEngine;
using UnityEngine.UI;

namespace BeggarEstateDefense
{
    public enum TypographyRole { Caption, Body, Button, Title, Hud }

    [RequireComponent(typeof(Text))]
    public sealed class ResponsiveTypography : MonoBehaviour
    {
        int baseSize;
        int lastWidth = -1;
        Text target;
        TypographyRole role;

        public void Configure(int size, TypographyRole typographyRole = TypographyRole.Body)
        {
            baseSize = size;
            role = typographyRole;
            target = GetComponent<Text>();
            Apply();
        }

        void OnEnable() { Apply(); }
        void Update()
        {
            if (lastWidth != Screen.width) Apply();
        }

        void Apply()
        {
            if (target == null) target = GetComponent<Text>();
            if (baseSize <= 0) baseSize = target.fontSize;
            lastWidth = Screen.width;
            float factor = Screen.width < 360 ? .90f : Screen.width >= 720 ? 1f : .94f;
            int minimum = role == TypographyRole.Caption ? 11 : role == TypographyRole.Body ? 12 : 14;
            target.fontSize = Mathf.Max(minimum, Mathf.RoundToInt(baseSize * factor));
        }
    }
}
