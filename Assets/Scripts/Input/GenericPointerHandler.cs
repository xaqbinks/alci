using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A generic handler for pointer-based input (PC mouse or VR controller).
/// It uses the platform-agnostic InputManager to cast rays and interact with UI elements.
/// </summary>
public class GenericPointerHandler : MonoBehaviour
{
    private GraphicRaycaster graphicRaycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    void Start()
    {
        // These components are typically found on the main UI Canvas.
        graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();

        if (graphicRaycaster == null || eventSystem == null)
        {
            Debug.LogError("GenericPointerHandler requires a GraphicRaycaster and an EventSystem in the scene. Make sure you have a UI Canvas.");
        }
    }

    void Update()
    {
        if (InputManager.instance == null) return;

        if (InputManager.instance.GetSelectDown())
        {
            HandleSelection();
        }
    }

    private void HandleSelection()
    {
        // Use the platform-agnostic pointer ray from the InputManager
        Ray pointerRay = InputManager.instance.GetPointerRay();

        // --- Step 1: UI Interaction (Graphic Raycast) ---
        // This method is primarily for screen-space UI (PC)
        pointerEventData = new PointerEventData(eventSystem);
        // We still need a screen position for this raycaster. In PC mode, this is the mouse.
        // In VR, this part of the code won't effectively hit anything, which is acceptable
        // as VR interaction should rely on the physics raycast below.
        pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2); // Center of screen as a fallback
        if (Application.isEditor && !PlatformRigManager.instance.IsVrActive)
        {
             pointerEventData.position = Input.mousePosition;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            Button button = results[0].gameObject.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log($"Pointer clicked on UI button: {button.name}");
                button.onClick.Invoke();
                return; // UI interaction takes precedence
            }
        }

        // --- Step 2: World Interaction (Physics Raycast) ---
        // This is the primary method for VR interaction with world-space UI that has colliders.
        RaycastHit hit;
        if (Physics.Raycast(pointerRay, out hit, 100f))
        {
            // In a more complex game, you could check for an "IInteractable" interface here.
            // For now, we'll just check if the hit object has a button component.
            Button button = hit.collider.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log($"Pointer clicked on World button: {button.name}");
                button.onClick.Invoke();
            }
        }
    }
}
