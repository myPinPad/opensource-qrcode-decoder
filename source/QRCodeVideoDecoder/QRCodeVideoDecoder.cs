/////////////////////////////////////////////////////////////////////
//
//	QR Code Library
//
//	QR Code video camera decoder.
//
//	Author: Uzi Granot
//	Original Version: 1.0
//	Date: June 30, 2018
//	Copyright (C) 2018-2019 Uzi Granot. All Rights Reserved
//	For full version history please look at QRDecoder.cs
//
//	QR Code Library C# class library and the attached test/demo
//  applications are free software.
//	Software developed by this author is licensed under CPOL 1.02.
//	Some portions of the QRCodeVideoDecoder are licensed under GNU Lesser
//	General Public License v3.0.
//
//	The solution is made of 3 projects:
//	1. QRCodeDecoderLibrary: QR code decoding.
//	3. QRCodeDecoderDemo: Decode QR code image files.
//	4. QRCodeVideoDecoder: Decode QR code using web camera.
//		This demo program is using some of the source modules of
//		Camera_Net project published at CodeProject.com:
//		https://www.codeproject.com/Articles/671407/Camera_Net-Library
//		and at GitHub: https://github.com/free5lot/Camera_Net.
//		This project is based on DirectShowLib.
//		http://sourceforge.net/projects/directshownet/
//		This project includes a modified subset of the source modules.
//
//	The main points of CPOL 1.02 subject to the terms of the License are:
//
//	Source Code and Executable Files can be used in commercial applications;
//	Source Code and Executable Files can be redistributed; and
//	Source Code can be modified to create derivative works.
//	No claim of suitability, guarantee, or any warranty whatsoever is
//	provided. The software is provided "as-is".
//	The Article accompanying the Work may not be distributed or republished
//	without the Author's consent
//
//	For version history please refer to QRDecoder.cs
/////////////////////////////////////////////////////////////////////

using DirectShowLib;
using QRCodeDecoderLibrary;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

namespace QRCodeVideoDecoder
{
/// <summary>
/// QR Code camera captuer using Direct Show Library
/// </summary>
public partial class QRCodeVideoDecoder : Form
	{
	private FrameSize FrameSize = new FrameSize(640, 480);
	private Camera VideoCamera;
	private Timer QRCodeTimer;
	private QRDecoder Decoder;

	/// <summary>
	/// Constructor
	/// </summary>
	public QRCodeVideoDecoder()
		{
		InitializeComponent();
		return;
		}

	/// <summary>
	/// Program initialization
	/// </summary>
	/// <param name="sender">Sender</param>
	/// <param name="e">Event arguments</param>
	private void OnLoad(object sender, EventArgs e)
		{
		// program title
		Text = "QRCodeVideoDecoder - " + QRDecoder.VersionNumber + " \u00a9 2013-2018 Uzi Granot. All rights reserved.";

		#if DEBUG
		// current directory
		string CurDir = Environment.CurrentDirectory;
		string WorkDir = CurDir.Replace("bin\\Debug", "Work");
		if(WorkDir != CurDir && Directory.Exists(WorkDir)) Environment.CurrentDirectory = WorkDir;

		// open trace file
		QRCodeTrace.Open("QRCodeVideoDecoderTrace.txt");
		QRCodeTrace.Write(Text);
		#endif

		// disable reset button
		ResetButton.Enabled = false;
		GoToUriButton.Enabled = false;

		// get an array of web camera devices
		DsDevice[] CameraDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

		// make sure at least one is available
		if(CameraDevices == null || CameraDevices.Length == 0)
			{
			MessageBox.Show("No video cameras in this computer");
			Close();
			return;
			}

		// select the first camera
		DsDevice CameraDevice = CameraDevices[0];

		// Device moniker
		IMoniker CameraMoniker = CameraDevice.Moniker;

		// get a list of frame sizes available
		FrameSize[] FrameSizes = Camera.GetFrameSizeList(CameraMoniker);

		// make sure there is at least one frame size
		if(FrameSizes == null || FrameSizes.Length == 0)
			{
			MessageBox.Show("No video cameras in this computer");
			Close();
			return;
			}

		// test if our frame size is available
		int Index;
		for(Index = 0; Index < FrameSizes.Length &&
			(FrameSizes[Index].Width != FrameSize.Width || FrameSizes[Index].Height != FrameSize.Height); Index++);

		// select first frame size
		if(Index == FrameSizes.Length) FrameSize = FrameSizes[0];

		// Set selected camera to camera control with default frame size
		// Create camera object
		VideoCamera = new Camera(PreviewPanel, CameraMoniker, FrameSize);

		// create QR code decoder
		Decoder = new QRDecoder();

		// resize window
		OnResize(sender, e);

		// create timer
		QRCodeTimer = new Timer();
		QRCodeTimer.Interval = 200;
		QRCodeTimer.Tick += QRCodeTimer_Tick;
		QRCodeTimer.Enabled = true;
		return;
		}

	private void QRCodeTimer_Tick(object sender, EventArgs e)
		{
		QRCodeTimer.Enabled = false;
		Bitmap QRCodeImage;
		try
			{
			QRCodeImage = VideoCamera.SnapshotSourceImage();

			// trace
			#if DEBUG
			QRCodeTrace.Format("Image width: {0}, Height: {1}", QRCodeImage.Width, QRCodeImage.Height);
			#endif
			}

		catch (Exception EX)
			{
			DataTextBox.Text = "Decode exception.\r\n" + EX.Message;
			QRCodeTimer.Enabled = true;
			return;
			}

		// decode image
		byte[][] DataByteArray = Decoder.ImageDecoder(QRCodeImage);
		string Text = QRCodeResult(DataByteArray);

		// dispose bitmap
		QRCodeImage.Dispose();

		// we have no QR code
		if(Text.Length == 0)
			{
			QRCodeTimer.Enabled = true;
			return;
			}

		VideoCamera.PauseGraph();

		DataTextBox.Text = Text;
		ResetButton.Enabled = true;
		if(IsValidUri(DataTextBox.Text)) GoToUriButton.Enabled = true;
		return;
		}

	/// <summary>
	/// Format result for display
	/// </summary>
	/// <param name="DataByteArray"></param>
	/// <returns></returns>
	private static string QRCodeResult
			(
			byte[][] DataByteArray
			)
		{
		// no QR code
		if(DataByteArray == null) return string.Empty;

		// image has one QR code
		if(DataByteArray.Length == 1) return QRDecoder.ByteArrayToStr(DataByteArray[0]);

		// image has more than one QR code
		StringBuilder Str = new StringBuilder();
		for(int Index = 0; Index < DataByteArray.Length; Index++)
			{
			if(Index != 0) Str.Append("\r\n");
			Str.AppendFormat("QR Code {0}\r\n", Index + 1);
			Str.Append(QRDecoder.ByteArrayToStr(DataByteArray[Index]));
			}
		return Str.ToString();
		}

	private static bool IsValidUri(string Uri)
		{
		if(!System.Uri.IsWellFormedUriString(Uri, UriKind.Absolute)) return false;

		if(!System.Uri.TryCreate(Uri, UriKind.Absolute, out Uri TempUri)) return false;

		return TempUri.Scheme == System.Uri.UriSchemeHttp || TempUri.Scheme == System.Uri.UriSchemeHttps;
		}

	/// <summary>
	/// Reset button was pressed
	/// </summary>
	/// <param name="sender">Sender</param>
	/// <param name="e">Event arguments</param>
	private void OnResetButton(object sender, EventArgs e)
		{
		VideoCamera.RunGraph();
		QRCodeTimer.Enabled = true;
		ResetButton.Enabled = false;
		GoToUriButton.Enabled = false;
		DataTextBox.Text = string.Empty;
		return;
		}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender">Sender</param>
	/// <param name="e">Event arguments</param>
	private void OnGoToUri(object sender, EventArgs e)
		{
		Process.Start(DataTextBox.Text);
		return;
		}

	/// <summary>
	/// Resize window
	/// </summary>
	/// <param name="sender">Sender</param>
	/// <param name="e">Event arguments</param>
	private void OnResize(object sender, EventArgs e)
		{
		// minimize
		if(ClientSize.Width == 0) return;

		// put reset button at bottom center
		ResetButton.Left = ClientSize.Width / 2 - ResetButton.Width - 8;
		ResetButton.Top = ClientSize.Height - ResetButton.Height - 8;
		GoToUriButton.Left = ResetButton.Right + 16;
		GoToUriButton.Top = ResetButton.Top;

		// data text box
		DataTextBox.Top = ResetButton.Top - DataTextBox.Height - 8;
		DataTextBox.Width = ClientSize.Width - 8 - SystemInformation.VerticalScrollBarWidth;

		// decoded data label
		DecodedDataLabel.Top = DataTextBox.Top - DecodedDataLabel.Height - 4;

		// preview area
		int AreaWidth = ClientSize.Width - 4;
		int AreaHeight = DecodedDataLabel.Top - 4;
		if(AreaHeight > FrameSize.Height * AreaWidth / FrameSize.Width)
			AreaHeight = FrameSize.Height * AreaWidth / FrameSize.Width;
		else
			AreaWidth = FrameSize.Width * AreaHeight / FrameSize.Height;

		// preview panel
		PreviewPanel.Left = (ClientSize.Width - AreaWidth) / 2;
		PreviewPanel.Top = (DecodedDataLabel.Top - 4 - AreaHeight) / 2;
		PreviewPanel.Width = AreaWidth;
		PreviewPanel.Height = AreaHeight;
		return;
		}

	private void OnClosing(object sender, FormClosingEventArgs e)
		{
		if(VideoCamera != null) VideoCamera.Dispose();
		return;
		}
	}
}
