using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionPromptUI : MonoBehaviour
{
    private const int RingTextureSize = 128;

    private static Sprite ringSprite;
    private static Sprite circleSprite;

    [Header("Scene References")]
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image ringBackgroundImage;
    [SerializeField] private Image ringProgressImage;
    [SerializeField] private Image keyBackgroundImage;
    [SerializeField] private Text keyText;
    [SerializeField] private Text actionText;

    private bool referencesBound;

    void Reset()
    {
        AutoAssignSceneReferences();
    }

    void OnValidate()
    {
        AutoAssignSceneReferences();
    }

    void Awake()
    {
        AutoAssignSceneReferences();
        CacheSceneReferences();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        CacheSceneReferences();
        if (!referencesBound)
            return;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        promptRoot.gameObject.SetActive(visible);
    }

    public void SetPrompt(string keyLabel, string actionLabel, float progress01, bool showProgress)
    {
        CacheSceneReferences();
        if (!referencesBound)
            return;

        if (keyText != null)
            keyText.text = string.IsNullOrWhiteSpace(keyLabel) ? "E" : keyLabel;

        if (actionText != null)
            actionText.text = actionLabel ?? string.Empty;

        if (ringProgressImage != null)
        {
            ringProgressImage.enabled = showProgress;
            ringProgressImage.fillAmount = Mathf.Clamp01(progress01);
        }
    }

    private void CacheSceneReferences()
    {
        AutoAssignSceneReferences();
        if (referencesBound)
            return;

        if (canvasRoot == null || promptRoot == null || ringProgressImage == null || keyText == null || actionText == null)
        {
            Debug.LogWarning(
                "InteractionPromptUI could not bind all scene references. Verify the prompt UI hierarchy under Player."
            );
            return;
        }

        if (canvasGroup == null)
            canvasGroup = canvasRoot.GetComponent<CanvasGroup>();

        if (ringBackgroundImage != null && ringBackgroundImage.sprite == null)
            ringBackgroundImage.sprite = GetRingSprite();

        if (ringProgressImage != null)
        {
            if (ringProgressImage.sprite == null)
                ringProgressImage.sprite = GetRingSprite();

            ringProgressImage.type = Image.Type.Filled;
            ringProgressImage.fillMethod = Image.FillMethod.Radial360;
            ringProgressImage.fillOrigin = (int)Image.Origin360.Top;
            ringProgressImage.fillClockwise = true;
        }

        if (keyBackgroundImage != null && keyBackgroundImage.sprite == null)
            keyBackgroundImage.sprite = GetCircleSprite();

        referencesBound = true;
    }

    private void AutoAssignSceneReferences()
    {
        canvasRoot ??= FindRectByPathOrName("InteractionPromptCanvas", "InteractionPromptCanvas");
        promptRoot ??= FindRectByPathOrName("InteractionPromptCanvas/PromptRoot", "PromptRoot");

        if (canvasGroup == null && canvasRoot != null)
            canvasGroup = canvasRoot.GetComponent<CanvasGroup>();

        ringBackgroundImage ??= FindImageByPathOrName(
            "InteractionPromptCanvas/PromptRoot/KeyRoot/RingBackground",
            "RingBackground"
        );
        ringProgressImage ??= FindImageByPathOrName(
            "InteractionPromptCanvas/PromptRoot/KeyRoot/RingProgress",
            "RingProgress"
        );
        keyBackgroundImage ??= FindImageByPathOrName(
            "InteractionPromptCanvas/PromptRoot/KeyRoot/KeyBackground",
            "KeyBackground"
        );
        keyText ??= FindTextByPathOrName(
            "InteractionPromptCanvas/PromptRoot/KeyRoot/KeyText",
            "KeyText"
        );
        actionText ??= FindTextByPathOrName(
            "InteractionPromptCanvas/PromptRoot/ActionLabel/Text",
            "ActionLabel"
        );
    }

    private RectTransform FindRectByPathOrName(string relativePath, string fallbackName)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found as RectTransform;

        return FindDescendantByName<RectTransform>(transform, fallbackName);
    }

    private Image FindImageByPathOrName(string relativePath, string fallbackName)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found.GetComponent<Image>();

        return FindDescendantByName<Image>(transform, fallbackName);
    }

    private Text FindTextByPathOrName(string relativePath, string fallbackName)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found.GetComponent<Text>();

        if (fallbackName == "ActionLabel")
        {
            Transform actionLabel = FindDescendantByName<Transform>(transform, fallbackName);
            if (actionLabel != null)
            {
                Text nestedText = actionLabel.GetComponentInChildren<Text>(true);
                if (nestedText != null)
                    return nestedText;
            }
        }

        return FindDescendantByName<Text>(transform, fallbackName);
    }

    private static T FindDescendantByName<T>(Transform root, string objectName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }

            T nested = FindDescendantByName<T>(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite == null)
            ringSprite = CreateCircleSprite(true);

        return ringSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
            circleSprite = CreateCircleSprite(false);

        return circleSprite;
    }

    private static Sprite CreateCircleSprite(bool ringOnly)
    {
        Texture2D texture = new Texture2D(RingTextureSize, RingTextureSize, TextureFormat.ARGB32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (RingTextureSize - 1) * 0.5f;
        float outerRadius = RingTextureSize * 0.48f;
        float innerRadius = ringOnly ? RingTextureSize * 0.34f : 0f;
        float feather = RingTextureSize * 0.03f;

        for (int y = 0; y < RingTextureSize; y++)
        {
            for (int x = 0; x < RingTextureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 0f;
                if (distance <= outerRadius)
                {
                    if (!ringOnly || distance >= innerRadius)
                    {
                        float outerFade = Mathf.InverseLerp(outerRadius, outerRadius - feather, distance);
                        float innerFade = ringOnly
                            ? Mathf.InverseLerp(innerRadius, innerRadius + feather, distance)
                            : 1f;
                        alpha = Mathf.Clamp01(outerFade * innerFade);
                    }
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, RingTextureSize, RingTextureSize),
            new Vector2(0.5f, 0.5f),
            RingTextureSize
        );
    }
}
