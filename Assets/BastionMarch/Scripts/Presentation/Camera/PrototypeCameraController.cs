using System;
using BastionMarch.Presentation.Bastions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

namespace BastionMarch.Presentation.CameraControl
{
    /// <summary>
    /// Управление камерой технической прототипной сцены.
    ///
    /// Камера относится только к Presentation и никак
    /// не изменяет состояние Simulation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeCameraController
        : MonoBehaviour
    {
        private const float MinimumPositiveValue = 0.01f;

        private readonly List<RaycastResult>
            _pointerRaycastResults = new();

        [Header("References")]

        [SerializeField]
        private UnityEngine.Camera _camera;

        [SerializeField]
        private BastionView _bastionView;

        [SerializeField]
        private BastionGridLayout _layout;

        [Header("Pan")]

        [SerializeField]
        [Min(MinimumPositiveValue)]
        private float _keyboardPanSpeed = 10f;

        [SerializeField]
        [Min(MinimumPositiveValue)]
        private float _keyboardZoomScaling = 0.75f;

        [Header("Zoom")]

        [Tooltip(
            "Доля изменения масштаба за один шаг колеса. " +
            "0.2 означает примерно 20%.")]
        [SerializeField]
        [Range(0.05f, 0.5f)]
        private float _zoomFractionPerStep = 0.2f;

        [SerializeField]
        [Min(MinimumPositiveValue)]
        private float _minimumOrthographicSize = 1.25f;

        [SerializeField]
        [Min(MinimumPositiveValue)]
        private float _maximumOrthographicSize = 20f;

        [Header("Bounds")]

        [SerializeField]
        [Min(0f)]
        private float _boundsPadding = 1f;

        [SerializeField]
        private bool _fitBastionOnStart = true;

        private bool _initialFitCompleted;

        private void Reset()
        {
            ResolveReferences();

            _keyboardPanSpeed = 10f;
            _keyboardZoomScaling = 0.75f;
            _zoomFractionPerStep = 0.2f;
            _minimumOrthographicSize = 1.25f;
            _maximumOrthographicSize = 20f;
            _boundsPadding = 1f;
            _fitBastionOnStart = true;
        }

        private void Awake()
        {
            ResolveReferences();

            if (!_camera.orthographic)
            {
                throw new InvalidOperationException(
                    "Prototype camera must use Orthographic projection.");
            }
        }

        private void LateUpdate()
        {
            if (!_initialFitCompleted &&
                _fitBastionOnStart &&
                _bastionView.IsBound)
            {
                FitBastionToScreen();
                _initialFitCompleted = true;
            }

            HandleKeyboardInput();
            HandleMouseDrag();
            HandleMouseZoom();
            HandleFitShortcut();

            ClampCameraToBastion();
        }

        public void FitBastionToScreen()
        {
            ResolveReferences();

            if (!_bastionView.IsBound)
            {
                return;
            }

            GetBastionWorldBounds(
                out Vector2 minimum,
                out Vector2 maximum);

            float worldWidth =
                maximum.x - minimum.x;

            float worldHeight =
                maximum.y - minimum.y;

            float requiredSizeByHeight =
                worldHeight * 0.5f;

            float requiredSizeByWidth =
                worldWidth /
                (2f * _camera.aspect);

            _camera.orthographicSize =
                Mathf.Clamp(
                    Mathf.Max(
                        requiredSizeByHeight,
                        requiredSizeByWidth),
                    _minimumOrthographicSize,
                    _maximumOrthographicSize);

            Vector2 center =
                (minimum + maximum) * 0.5f;

            SetCameraPosition(center);
        }

        private void HandleKeyboardInput()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            Vector2 direction =
                Vector2.zero;

            if (keyboard.aKey.isPressed ||
                keyboard.leftArrowKey.isPressed)
            {
                direction.x -= 1f;
            }

            if (keyboard.dKey.isPressed ||
                keyboard.rightArrowKey.isPressed)
            {
                direction.x += 1f;
            }

            if (keyboard.sKey.isPressed ||
                keyboard.downArrowKey.isPressed)
            {
                direction.y -= 1f;
            }

            if (keyboard.wKey.isPressed ||
                keyboard.upArrowKey.isPressed)
            {
                direction.y += 1f;
            }

            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            direction.Normalize();

            float zoomAdjustedSpeed =
                _keyboardPanSpeed *
                Mathf.Max(
                    1f,
                    _camera.orthographicSize *
                    _keyboardZoomScaling);

            Vector3 movement =
                new Vector3(
                    direction.x,
                    direction.y,
                    0f) *
                zoomAdjustedSpeed *
                Time.unscaledDeltaTime;

            _camera.transform.position +=
                movement;
        }

        private void HandleMouseDrag()
        {
            Mouse mouse =
                Mouse.current;

            if (mouse == null ||
                !mouse.middleButton.isPressed ||
                IsPointerOverUi())
            {
                return;
            }

            Vector2 screenDelta =
                mouse.delta.ReadValue();

            if (screenDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            float worldHeight =
                _camera.orthographicSize * 2f;

            float worldUnitsPerPixel =
                worldHeight /
                Mathf.Max(
                    1f,
                    _camera.pixelHeight);

            Vector3 worldDelta =
                new Vector3(
                    -screenDelta.x *
                        worldUnitsPerPixel,
                    -screenDelta.y *
                        worldUnitsPerPixel,
                    0f);

            _camera.transform.position +=
                worldDelta;
        }

        private static float NormalizeScrollDelta(
            float scroll)
        {
            // На Windows обычное колесо часто возвращает ±120.
            // Некоторые мыши и системы возвращают ±1
            // либо промежуточные значения.
            if (Mathf.Abs(scroll) >= 10f)
            {
                return scroll / 120f;
            }

            return scroll;
        }

        private void HandleMouseZoom()
        {
            Mouse mouse =
                Mouse.current;

            if (mouse == null ||
                IsPointerOverUi())
            {
                return;
            }

            float scroll =
                mouse.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) <=
                Mathf.Epsilon)
            {
                return;
            }

            Vector2 cursorPosition =
                mouse.position.ReadValue();

            Vector3 worldBeforeZoom =
                ScreenToWorldOnBastionPlane(
                    cursorPosition);

            float normalizedScroll =
                NormalizeScrollDelta(scroll);

            float zoomBase =
                1f - _zoomFractionPerStep;

            float zoomMultiplier =
                Mathf.Pow(
                    zoomBase,
                    normalizedScroll);

            float targetSize =
                _camera.orthographicSize *
                zoomMultiplier;

            _camera.orthographicSize =
                Mathf.Clamp(
                    targetSize,
                    _minimumOrthographicSize,
                    _maximumOrthographicSize);

            Vector3 worldAfterZoom =
                ScreenToWorldOnBastionPlane(
                    cursorPosition);

            Vector3 correction =
                worldBeforeZoom -
                worldAfterZoom;

            correction.z = 0f;

            _camera.transform.position +=
                correction;
        }

        private void HandleFitShortcut()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard != null &&
                keyboard.fKey.wasPressedThisFrame)
            {
                FitBastionToScreen();
            }
        }

        private void ClampCameraToBastion()
        {
            if (!_bastionView.IsBound)
            {
                return;
            }

            GetBastionWorldBounds(
                out Vector2 minimum,
                out Vector2 maximum);

            float halfHeight =
                _camera.orthographicSize;

            float halfWidth =
                halfHeight *
                _camera.aspect;

            float centerX =
                ClampCenterCoordinate(
                    _camera.transform.position.x,
                    minimum.x,
                    maximum.x,
                    halfWidth);

            float centerY =
                ClampCenterCoordinate(
                    _camera.transform.position.y,
                    minimum.y,
                    maximum.y,
                    halfHeight);

            SetCameraPosition(
                new Vector2(
                    centerX,
                    centerY));
        }

        private void GetBastionWorldBounds(
            out Vector2 minimum,
            out Vector2 maximum)
        {
            float width =
                _bastionView.State.Width *
                _layout.CellWidth;

            float height =
                _bastionView.State.DeckCount *
                _layout.DeckHeight;

            Vector3 bottomLeft =
                _layout.transform.TransformPoint(
                    new Vector3(
                        _layout.LocalOrigin.x,
                        _layout.LocalOrigin.y,
                        0f));

            Vector3 topRight =
                _layout.transform.TransformPoint(
                    new Vector3(
                        _layout.LocalOrigin.x +
                            width,
                        _layout.LocalOrigin.y +
                            height,
                        0f));

            minimum =
                new Vector2(
                    Mathf.Min(
                        bottomLeft.x,
                        topRight.x) -
                    _boundsPadding,
                    Mathf.Min(
                        bottomLeft.y,
                        topRight.y) -
                    _boundsPadding);

            maximum =
                new Vector2(
                    Mathf.Max(
                        bottomLeft.x,
                        topRight.x) +
                    _boundsPadding,
                    Mathf.Max(
                        bottomLeft.y,
                        topRight.y) +
                    _boundsPadding);
        }

        private Vector3 ScreenToWorldOnBastionPlane(
            Vector2 screenPosition)
        {
            float distanceToPlane =
                Mathf.Abs(
                    _camera.transform.position.z -
                    _layout.transform.position.z);

            return _camera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    distanceToPlane));
        }

        private void SetCameraPosition(
            Vector2 position)
        {
            Vector3 current =
                _camera.transform.position;

            _camera.transform.position =
                new Vector3(
                    position.x,
                    position.y,
                    current.z);
        }

        private static float ClampCenterCoordinate(
            float current,
            float minimum,
            float maximum,
            float cameraHalfSize)
        {
            float minimumCenter =
                minimum + cameraHalfSize;

            float maximumCenter =
                maximum - cameraHalfSize;

            if (minimumCenter >
                maximumCenter)
            {
                return
                    (minimum + maximum) *
                    0.5f;
            }

            return Mathf.Clamp(
                current,
                minimumCenter,
                maximumCenter);
        }

        private bool IsPointerOverUi()
        {
            EventSystem eventSystem =
                EventSystem.current;

            Mouse mouse =
                Mouse.current;

            if (eventSystem == null ||
                mouse == null)
            {
                return false;
            }

            var pointerEventData =
                new PointerEventData(eventSystem)
                {
                    position =
                        mouse.position.ReadValue()
                };

            _pointerRaycastResults.Clear();

            eventSystem.RaycastAll(
                pointerEventData,
                _pointerRaycastResults);

            foreach (
                RaycastResult raycastResult
                in _pointerRaycastResults)
            {
                // Учитываем только элементы Canvas.
                // Physics2DRaycaster игровых модулей
                // не должен блокировать камеру.
                if (raycastResult.module
                    is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (_camera == null)
            {
                _camera =
                    UnityEngine.Camera.main;
            }

            Transform prototypeRoot =
                transform.parent != null
                    ? transform.parent.parent
                    : null;

            if (_bastionView == null &&
                prototypeRoot != null)
            {
                _bastionView =
                    prototypeRoot
                        .GetComponentInChildren<
                            BastionView>(
                                includeInactive: true);
            }

            if (_layout == null &&
                _bastionView != null)
            {
                _layout =
                    _bastionView
                        .GetComponent<
                            BastionGridLayout>();
            }

            if (_camera == null)
            {
                throw new InvalidOperationException(
                    "PrototypeCameraController requires Camera.");
            }

            if (_bastionView == null)
            {
                throw new InvalidOperationException(
                    "PrototypeCameraController requires BastionView.");
            }

            if (_layout == null)
            {
                throw new InvalidOperationException(
                    "PrototypeCameraController requires BastionGridLayout.");
            }
        }

        private void OnValidate()
        {
            _keyboardPanSpeed =
                Mathf.Max(
                    MinimumPositiveValue,
                    _keyboardPanSpeed);

            _keyboardZoomScaling =
                Mathf.Max(
                    MinimumPositiveValue,
                    _keyboardZoomScaling);

            _zoomFractionPerStep =
                Mathf.Clamp(
                    _zoomFractionPerStep,
                    0.05f,
                    0.5f);

            _minimumOrthographicSize =
                Mathf.Max(
                    MinimumPositiveValue,
                    _minimumOrthographicSize);

            _maximumOrthographicSize =
                Mathf.Max(
                    _minimumOrthographicSize,
                    _maximumOrthographicSize);

            _boundsPadding =
                Mathf.Max(
                    0f,
                    _boundsPadding);
        }
    }
}