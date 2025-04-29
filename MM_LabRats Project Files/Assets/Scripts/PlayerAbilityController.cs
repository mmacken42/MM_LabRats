using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilityController : MonoBehaviour
{
    public Text abilityText;
    public string defaultText;
    public float cooldownTimeSeconds = 1f;
    private float cooldownTimer;
    private bool coolingDown;

    private void Update()
    {
        if (coolingDown)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                abilityText.text = Mathf.FloorToInt(cooldownTimer).ToString();
            }
            else
            {
                ResetAbility();
            }
        }
    }

    public void StartAbilityCooldown()
    {
        coolingDown = true;
        cooldownTimer = cooldownTimeSeconds;
    }

    public void ResetAbility()
    {
        coolingDown = false;
        abilityText.text = defaultText;
        cooldownTimer = 0;
    }

    public bool CanUseAbility()
    {
        return !coolingDown;
    }
}
