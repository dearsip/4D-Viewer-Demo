﻿using UnityEngine;
using UnityEngine.UI;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(BoxCollider))]
    public class ScrollDrive : LinearDrive
    {
        public Scrollbar scrollbar;
        BoxCollider boxCollider;
        public RectTransform rectTransform;
        Vector3 size = new Vector3();

        protected override void Start()
        {
            initialMappingOffset = linearMapping.value;
            boxCollider = GetComponent<BoxCollider>();
            size = rectTransform.rect.size;
            size.z = 5;
            linearMapping.value = scrollbar.value;
        }

        protected override void Update()
        {
            base.Update();
            size.y = rectTransform.rect.height;
            boxCollider.size = size;

            if (repositionGameObject)
            {
                transform.position = Vector3.Lerp(startPosition.position, endPosition.position, linearMapping.value);
            }

            linearMapping.value = scrollbar.value;
        }

        protected override void OnDetachedFromHand(Hand hand)
        {
        }

        protected override void UpdateLinearMapping(Transform updateTransform)
        {
            prevMapping = linearMapping.value;
            linearMapping.value = Mathf.Clamp01(initialMappingOffset + CalculateLinearMapping(updateTransform));

            mappingChangeSamples[sampleCount % mappingChangeSamples.Length] = (1.0f / Time.deltaTime) * (linearMapping.value - prevMapping);
            sampleCount++;

            scrollbar.value = linearMapping.value;

            if (repositionGameObject)
            {
                transform.position = Vector3.Lerp(startPosition.position, endPosition.position, linearMapping.value);
            }

        }
    }
}