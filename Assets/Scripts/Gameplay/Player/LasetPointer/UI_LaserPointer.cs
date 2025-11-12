//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    public class UI_LaserPointer : MonoBehaviour
    {
        public SteamVR_Behaviour_Pose pose;

        //public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.__actions_default_in_InteractUI;
        public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");

        public Color color;
        public float thickness = 0.002f;
        public Color clickColor = Color.green;
        public GameObject holder;
        public GameObject pointer;
        bool isActive = false;
        bool isEnabled = false;
        public bool addRigidBody = false;
        public Transform reference;
        private UIElement _hoveringInteractable = null;

        public UIElement hoveringInteractable
        {
            get { return _hoveringInteractable; }
            set
            {
                if (_hoveringInteractable != value)
                {
                    if (_hoveringInteractable != null)
                    {
                        OnPointerOut();
                        _hoveringInteractable.SendMessage("OnHandHoverEnd", this.gameObject.GetComponent<Hand>(), SendMessageOptions.DontRequireReceiver);
                    }

                    _hoveringInteractable = value;

                    if (_hoveringInteractable != null)
                    {
                        OnPointerIn();
                        _hoveringInteractable.gameObject.SendMessage("OnHandHoverBegin", this.gameObject.GetComponent<Hand>(), SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }



        void OnEnable()
        {
            if (isEnabled)
                return;

            isEnabled = true;

            if (pose == null)
                pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object", this);

            if (interactWithUI == null)
                Debug.LogError("No ui interaction action has been set on this component.", this);


            holder = new GameObject();
            holder.transform.parent = this.transform;
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;

            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.layer = LayerMask.NameToLayer("VRUI");
            pointer.transform.parent = holder.transform;
            pointer.transform.localScale = new Vector3(thickness, thickness, 100f);
            pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointer.transform.localRotation = Quaternion.identity;
            BoxCollider collider = pointer.GetComponent<BoxCollider>();
            if (addRigidBody)
            {
                if (collider)
                {
                    collider.isTrigger = true;
                }
                Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }
            else
            {
                if (collider)
                {
                    Object.Destroy(collider);
                }
            }
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            newMaterial.SetColor("_Color", color);
            pointer.GetComponent<MeshRenderer>().material = newMaterial;
        }
        
        void OnDisable()
        {
            if (holder != null)
            {
                Destroy(holder);
            }
            if (pointer != null)
            {
                Destroy(pointer);
            }
            hoveringInteractable = null;

            isActive = false;
            isEnabled = false;
        }

        public virtual void OnPointerIn()
        {
        }

        public virtual void OnPointerClick()
        {
            if(hoveringInteractable != null)
            {
                hoveringInteractable.SendMessage("OnButtonClick", SendMessageOptions.DontRequireReceiver);
            }
        }

        public virtual void OnPointerOut()
        {
        }


        private void Update()
        {
            if (!isActive)
            {
                isActive = true;
                this.transform.GetChild(0).gameObject.SetActive(true);
            }

            float dist = 100f;

            Ray raycast = new Ray(transform.position, transform.forward);
            RaycastHit hit;
            bool bHit = Physics.Raycast(raycast, out hit);

            if (bHit)
            {
                hoveringInteractable = hit.collider.GetComponentInParent<UIElement>();
            }
            else
            {
                hoveringInteractable = null;
            }

            if (bHit && hit.distance < 100f)
            {
                dist = hit.distance;
            }

            if (bHit && interactWithUI.GetStateUp(pose.inputSource))
            {
                OnPointerClick();
            }

            // Change Color of Ray
            if (interactWithUI != null && interactWithUI.GetState(pose.inputSource))
            {
                pointer.transform.localScale = new Vector3(thickness * 5f, thickness * 5f, dist);
                pointer.GetComponent<MeshRenderer>().material.color = clickColor;
            }
            else
            {
                pointer.transform.localScale = new Vector3(thickness, thickness, dist);
                pointer.GetComponent<MeshRenderer>().material.color = color;
            }
            // Change Length of Ray
            pointer.transform.localPosition = new Vector3(0f, 0f, dist / 2f);
        }
    }

}