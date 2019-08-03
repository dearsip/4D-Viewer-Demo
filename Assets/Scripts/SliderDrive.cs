//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Drives a linear mapping based on position between 2 positions
//
//=============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    [RequireComponent(typeof(Interactable))]
    public class SliderDrive : LinearDrive
    {
        public Slider slider;

        protected override void OnDetachedFromHand(Hand hand)
        {
        }

        protected override void UpdateLinearMapping(Transform updateTransform)
        {
            prevMapping = linearMapping.value;
            linearMapping.value = Mathf.Clamp01(initialMappingOffset + CalculateLinearMapping(updateTransform));

            mappingChangeSamples[sampleCount % mappingChangeSamples.Length] = (1.0f / Time.deltaTime) * (linearMapping.value - prevMapping);
            sampleCount++;

            if (repositionGameObject)
            {
                transform.position = Vector3.Lerp(startPosition.position, endPosition.position, linearMapping.value);
            }

            slider.value = linearMapping.value;
        }
    }
}