using UnityEngine;
using UnityEngine.UIElements;

public class PoopCooldownBar : MonoBehaviour
{
    private UIDocument myUIdoc;
    private VisualElement root;
    private VisualElement background;
    private ProgressBar myProgressBar;

    public float cooldownTimeSeconds = 4f;

    private bool coolingDown;
    private float cooldownTimer;

    //public Color backgroundColor;

    void Start()
    {
        myUIdoc = GetComponent<UIDocument>();
        root = myUIdoc.rootVisualElement;
        //background = root.Q<VisualElement>(".unity-progress-bar__background");
        //background.style.unityBackgroundImageTintColor = new StyleColor(backgroundColor);
        myProgressBar = root.Q<ProgressBar>("ProgressBar");

        ResetAbility();

        //gameObject.SetActive(false);
    }

    void Update()
    {
        if (coolingDown)
        {
            if (cooldownTimer < cooldownTimeSeconds)
            {
                cooldownTimer += Time.deltaTime;
                myProgressBar.value = (cooldownTimer / cooldownTimeSeconds) * 100;
            }
            else
            {
                ResetAbility();
            }
        }
    }

    public void StartAbilityCooldown()
    {
        myProgressBar.value = 0;
        myProgressBar.title = " ";
        coolingDown = true;
    }

    public bool CanUseAbility()
    {
        return !coolingDown;
    }

    public void ResetAbility()
    {
        coolingDown = false;
        myProgressBar.value = 100;
        myProgressBar.title = "Q = Poop";
        cooldownTimer = 0;
    }

    public void ToggleBarVisibile(bool isVisible)
    {
        //myUIdoc = GetComponent<UIDocument>();
        //myUIdoc.enabled = isVisible;
    }
}
