﻿using System;

using UIKit;
using Foundation;
using AVFoundation;
using System.Threading.Tasks;

namespace Camera
{
	public partial class ViewController : UIViewController
	{
		bool flashOn = false;

		AVCaptureSession captureSession;
		AVCaptureDeviceInput captureDeviceInput;
		AVCaptureStillImageOutput stillImageOutput;
		AVCaptureVideoPreviewLayer videoPreviewLayer;

		public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override async void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			await AuthorizeCameraUse ();
			SetupLiveCameraStream ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

		async partial void TakePhotoButtonTapped (UIButton sender)
		{
			var videoConnection = stillImageOutput.ConnectionFromMediaType (AVMediaType.Video);
			var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync (videoConnection);

			var jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData (sampleBuffer);
			var jpegAsByteArray = jpegImageAsNsData.ToArray ();

		    // TODO: Send this to local storage or cloud storage such as Azure Storage.
		}

		partial void SwitchCameraButtonTapped (UIButton sender)
		{
			var devicePosition = captureDeviceInput.Device.Position;
			if (devicePosition == AVCaptureDevicePosition.Front) {
				devicePosition = AVCaptureDevicePosition.Back;
			} else {
				devicePosition = AVCaptureDevicePosition.Front;
			}

			var device = GetCameraForOrientation (devicePosition);
			ConfigureCameraForDevice (device);

			captureSession.BeginConfiguration ();
			captureSession.RemoveInput (captureDeviceInput);
			captureDeviceInput = AVCaptureDeviceInput.FromDevice (device);
			captureSession.AddInput (captureDeviceInput);
			captureSession.CommitConfiguration ();
		}

		public AVCaptureDevice GetCameraForOrientation (AVCaptureDevicePosition orientation)
		{
			var devices = AVCaptureDevice.DevicesWithMediaType (AVMediaType.Video);

			foreach (var device in devices) {
				if (device.Position == orientation) {
					return device;
				}
			}

			return null;
		}

		partial void FlashButtonTapped (UIButton sender)
		{
			var device = captureDeviceInput.Device;

			var error = new NSError ();
			if (device.HasFlash) {
				if (device.FlashMode == AVCaptureFlashMode.On) {
					device.LockForConfiguration (out error);
					device.FlashMode = AVCaptureFlashMode.Off;
					device.UnlockForConfiguration ();

					flashButton.SetBackgroundImage (UIImage.FromBundle ("NoFlashButton.png"), UIControlState.Normal);
				} else {
					device.LockForConfiguration (out error);
					device.FlashMode = AVCaptureFlashMode.On;
					device.UnlockForConfiguration ();

					flashButton.SetBackgroundImage (UIImage.FromBundle ("FlashButton.png"), UIControlState.Normal);
				}
			}

			flashOn = !flashOn;
		}

		async Task AuthorizeCameraUse ()
		{
			var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus (AVMediaType.Video);

			if (authorizationStatus != AVAuthorizationStatus.Authorized) {
				await AVCaptureDevice.RequestAccessForMediaTypeAsync (AVMediaType.Video);
			}
		}

		public void SetupLiveCameraStream ()
		{
			captureSession = new AVCaptureSession ();

			var viewLayer = liveCameraStream.Layer;
			videoPreviewLayer = new AVCaptureVideoPreviewLayer (captureSession) {
				Frame = this.View.Frame
			};
			liveCameraStream.Layer.AddSublayer (videoPreviewLayer);

			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType (AVMediaType.Video);
			ConfigureCameraForDevice (captureDevice);
			captureDeviceInput = AVCaptureDeviceInput.FromDevice (captureDevice);
			captureSession.AddInput (captureDeviceInput);

			var dictionary = new NSMutableDictionary();
			dictionary[AVVideo.CodecKey] = new NSNumber((int) AVVideoCodec.JPEG);
			stillImageOutput = new AVCaptureStillImageOutput () {
				OutputSettings = new NSDictionary ()
			};

			captureSession.AddOutput (stillImageOutput);
			captureSession.StartRunning ();
		}

		void ConfigureCameraForDevice (AVCaptureDevice device)
		{
			var error = new NSError ();
			if (device.IsFocusModeSupported (AVCaptureFocusMode.ContinuousAutoFocus)) {
				device.LockForConfiguration (out error);
				device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
				device.UnlockForConfiguration ();
			} else if (device.IsExposureModeSupported (AVCaptureExposureMode.ContinuousAutoExposure)) {
				device.LockForConfiguration (out error);
				device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
				device.UnlockForConfiguration ();
			} else if (device.IsWhiteBalanceModeSupported (AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance)) {
				device.LockForConfiguration (out error);
				device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
				device.UnlockForConfiguration ();
			}
		}
	}
}