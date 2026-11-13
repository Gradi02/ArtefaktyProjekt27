using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.StatusSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnitStatusPanel : MonoBehaviour
{
    private TextMeshProUGUI _roleText;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _healthText;
    private TextMeshProUGUI _attackText;
    private TextMeshProUGUI _statusesText;
    private Image _healthFill;
    private LivingMixin _unit;

    public static UnitStatusPanel Create(
        Transform parent,
        string objectName,
        string role,
        LivingMixin unit,
        Vector2 size)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.sizeDelta = size;
        panelRect.pivot = new Vector2(0f, 0f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        background.raycastTarget = false;

        UnitStatusPanel panel = panelObject.AddComponent<UnitStatusPanel>();
        panel.BuildContents(role);
        panel.Bind(unit);
        return panel;
    }

    public void Bind(LivingMixin unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException(nameof(unit));
        }

        _unit = unit;
        _nameText.text = unit.name;
        Refresh();
    }

    public void Refresh()
    {
        if (_unit == null)
        {
            return;
        }

        _healthText.text = $"HP: {_unit.health}/{_unit.maxHealth}";
        _healthFill.fillAmount = _unit.maxHealth <= 0
            ? 0f
            : Mathf.Clamp01((float)_unit.health / _unit.maxHealth);
        _healthFill.color = GetHealthColor(_healthFill.fillAmount);
        _attackText.text = $"Atak: {_unit.AttackPower.CalculateValue().ToString("0.##", CultureInfo.InvariantCulture)}";
        _statusesText.text = GetStatusSummary(_unit);
    }

    private void BuildContents(string role)
    {
        RectTransform panelRect = (RectTransform)transform;
        _roleText = CreateText(panelRect, "Role", role, 15, new Vector2(20f, -12f), new Vector2(340f, 22f));
        _roleText.color = new Color(0.7f, 0.78f, 0.9f);
        _nameText = CreateText(panelRect, "Name", string.Empty, 24, new Vector2(20f, -37f), new Vector2(340f, 30f));
        _healthText = CreateText(panelRect, "Health", string.Empty, 18, new Vector2(20f, -77f), new Vector2(340f, 24f));
        CreateHealthBar(panelRect);
        _attackText = CreateText(panelRect, "Attack", string.Empty, 17, new Vector2(20f, -132f), new Vector2(340f, 24f));
        _statusesText = CreateText(panelRect, "Statuses", string.Empty, 15, new Vector2(20f, -160f), new Vector2(340f, 62f));
        _statusesText.textWrappingMode = TextWrappingModes.Normal;
        _statusesText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void CreateHealthBar(RectTransform parent)
    {
        RectTransform backgroundRect = CreateRect(
            parent,
            "Health Bar",
            new Vector2(20f, -104f),
            new Vector2(340f, 16f));
        Image background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.2f, 0.24f, 1f);
        background.raycastTarget = false;

        RectTransform fillRect = CreateRect(
            backgroundRect,
            "Fill",
            Vector2.zero,
            Vector2.zero);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        _healthFill = fillRect.gameObject.AddComponent<Image>();
        _healthFill.type = Image.Type.Filled;
        _healthFill.fillMethod = Image.FillMethod.Horizontal;
        _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _healthFill.color = new Color(0.25f, 0.8f, 0.4f, 1f);
        _healthFill.raycastTarget = false;
    }

    private static string GetStatusSummary(LivingMixin unit)
    {
        IReadOnlyList<StatusInstance> statuses = ((IStateEntity)unit).ActiveStatuses;
        if (statuses.Count == 0)
        {
            return "Statusy: brak";
        }

        StringBuilder builder = new StringBuilder("Statusy: ");
        for (int index = 0; index < statuses.Count; index++)
        {
            StatusInstance status = statuses[index];
            if (index > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(status.Definition.Name);
            builder.Append(" [");
            builder.Append(status.Definition.Type);
            builder.Append(']');
            builder.Append(" x");
            builder.Append(status.Stacks);
            builder.Append(" (");
            builder.Append(status.IsPermanent ? "stały" : $"{status.DurationTurns} t.");
            builder.Append(')');
        }

        return builder.ToString();
    }

    private static Color GetHealthColor(float ratio)
    {
        if (ratio > 0.5f)
        {
            return new Color(0.25f, 0.8f, 0.4f, 1f);
        }
        if (ratio > 0.25f)
        {
            return new Color(0.95f, 0.72f, 0.2f, 1f);
        }

        return new Color(0.88f, 0.24f, 0.2f, 1f);
    }

    private static TextMeshProUGUI CreateText(
        RectTransform parent,
        string objectName,
        string text,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
