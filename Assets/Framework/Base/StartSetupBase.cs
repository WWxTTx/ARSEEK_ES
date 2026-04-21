using UnityFramework.Runtime;
using UnityEngine;

namespace UnityFramework.Runtime
{
    public class StartSetupBase : Singleton<StartSetupBase>
    {
        public Transform Managers;

        protected override void InstanceAwake()
        {
            base.InstanceAwake();

            new GameObject(typeof(ResLoad).Name).AddComponent<ResLoad>().transform.parent = Managers;
            new GameObject(typeof(FormMsgManager).Name).AddComponent<FormMsgManager>().transform.parent = Managers;
            new GameObject(typeof(UIManager).Name).AddComponent<UIManager>().transform.parent = Managers;
        }
    }
}
