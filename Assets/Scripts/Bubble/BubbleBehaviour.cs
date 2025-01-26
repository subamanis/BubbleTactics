using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Prefabs.Scripts
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BubbleBehaviour : MonoBehaviour
    {
        public float exciteTimeTransitionSeconds = 1f;

        public float exciteScrollingSpeed = 1f;
        public float exciteSurfaceMovementSpeed = 0.05f;
        public float exciteNoiseScale = 0.4f;

        public float normalScrollingSpeed = 0.05f;
        public float normalSurfaceMovementSpeed = 0f;
        public float normalNoiseScale = 0f;

        private static readonly int ScrollingSpeed = Shader.PropertyToID("_ScrollingSpeed");
        private static readonly int SurfaceMovementSpeed = Shader.PropertyToID("_SurfaceMovementSpeed");
        private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");

        private Material _bubbleMaterial;
        
        public TMP_Text bubbleUserName;

        private void Start()
        {
            // Get the material of the meshRenderer
            var currentBubbleMaterial = this.GetComponent<MeshRenderer>().material;
            // Create a new runtime instance and set it
            this._bubbleMaterial = new Material(currentBubbleMaterial);
            this.GetComponent<MeshRenderer>().material = this._bubbleMaterial;
        }

        IEnumerator ExciteBubble(bool reverse = false)
        {
            // Start an enumerator to linearly change the variable in a specified time
            var time = 0f;
            while (time < exciteTimeTransitionSeconds)
            {
                time += Time.deltaTime;

                // Interpolate normally or in reverse
                var t = time / exciteTimeTransitionSeconds;
                if (reverse)
                {
                    this._bubbleMaterial.SetFloat(ScrollingSpeed,
                        Mathf.Lerp(this.exciteScrollingSpeed, this.normalScrollingSpeed, t));
                    this._bubbleMaterial.SetFloat(SurfaceMovementSpeed,
                        Mathf.Lerp(this.exciteSurfaceMovementSpeed, this.normalSurfaceMovementSpeed, t));
                    this._bubbleMaterial.SetFloat(NoiseScale,
                        Mathf.Lerp(this.exciteNoiseScale, this.normalNoiseScale, t));
                }
                else
                {
                    this._bubbleMaterial.SetFloat(ScrollingSpeed,
                        Mathf.Lerp(this.normalScrollingSpeed, this.exciteScrollingSpeed, t));
                    this._bubbleMaterial.SetFloat(SurfaceMovementSpeed,
                        Mathf.Lerp(this.normalSurfaceMovementSpeed, this.exciteSurfaceMovementSpeed, t));
                    this._bubbleMaterial.SetFloat(NoiseScale,
                        Mathf.Lerp(this.normalNoiseScale, this.exciteNoiseScale, t));
                }

                yield return null;
            }
        }

        public void Normal()
        {
            StartCoroutine(ExciteBubble(true));
        }

        public void Excite()
        {
            StartCoroutine(ExciteBubble());
        }

        public void SetPlayerUserName(string userName)
        {
           this.bubbleUserName.text = userName;
           this.bubbleUserName.gameObject.SetActive(true);
        }
    }
}