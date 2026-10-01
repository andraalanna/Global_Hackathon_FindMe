using UnityEngine;

namespace FindMe.UI
{
    /// <summary>Shows the little lemon crown on the Family tab only while the Care plan is not active.</summary>
    public class CarePlanBadge : MonoBehaviour
    {
        public UIManager app;
        public GameObject dot;

        void OnEnable()
        {
            if (app != null && dot != null) dot.SetActive(!app.State.carePlan);
        }
    }
}
