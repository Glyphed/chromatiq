using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using VoxelBusters.NativePlugins;


public class PlayFromWebCam : MonoBehaviour {

	// VARS
	public RawImage rawimage;
	public WebCamTexture webcamTexture;
	public Camera Cam;
	public int ax;
	public int ay;
	public float cr, cg, cb;
	public string logtext;
	public Image aimImage;
	// Define vars to hold RGB values
	public float REDValue;
	public float GREENValue;
	public float BLUEValue;
	public Texture2D tex;
	public float marginX = 10f;
	public float marginY = 0f;
	public Color currentPixel, colHSV;
	public Color32 convertedColor;
	public Button buttonColorInfo, buttonLock, buttonInfo, buttonAnalyze, buttonSettings;
	public Slider sliderSettings;
	public float scaleRatio;
	public bool pausedMode = false;
	public float hh, ss, vv = 0f;
	private string colorReport, intensityDetect;
	private bool sliderVisible = false;
	private float screenRefWidth = 480f; // Reference screen width in pixels
	private float buttonSpacing = Screen.width/30f; // Space between buttons
	private float buttonRefSize = 100f; // Reference button size in pixels 
	private float buttonOriginalSize = 360f; // Original size of the button object, in pixels
	private float buttonRefScale;
	private float sampleSize = 4f;
	private float totalSampledPixels = 9f;
	public Sprite originalImage;
	public Sprite checkImage;
	public Sprite lock1Image;
	public Sprite lock2Image;
	private string hexVal;

	// Initialization
	void Start ()
	{	
		// Check if this is the first time user opened the app and show custom introductory popup
		if (PlayerPrefs.GetString("FirstTime")!="nope")
		{ 
			PlayerPrefs.SetString("FirstTime", "nope"); 
			NPBinding.UI.ShowAlertDialogWithSingleButton("Welcome", "First time user popup. \n \n Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ornare dictum nibh, quis vehicula justo sodales at.", "Close", null);
		}

		// Position UI elements in relation to device screen
		sliderSettings.interactable = false;
		sliderSettings.transform.position = new Vector3(0, -500, 1);
		
		buttonRefScale = buttonOriginalSize/buttonRefSize;
		scaleRatio = Screen.width/screenRefWidth;
		float buttonScale = (1/4.8f)*buttonRefScale;

		buttonInfo.transform.localScale = Vector3.Scale(buttonInfo.transform.localScale, new Vector3(buttonScale, buttonScale, 1));
		buttonSettings.transform.localScale = Vector3.Scale(buttonSettings.transform.localScale, new Vector3(buttonScale, buttonScale, 1));
		buttonLock.transform.localScale = Vector3.Scale(buttonLock.transform.localScale, new Vector3(buttonScale, buttonScale, 1));
		buttonAnalyze.transform.localScale = Vector3.Scale(buttonAnalyze.transform.localScale, new Vector3(buttonScale, buttonScale, 1));

		buttonInfo.transform.position = new Vector3(buttonSpacing, buttonSpacing, 0);
		buttonSettings.transform.position = new Vector3(buttonInfo.transform.position.x + Screen.width/4.8f + buttonSpacing, buttonSpacing, 0);
		buttonLock.transform.position = new Vector3(buttonSettings.transform.position.x + Screen.width/4.8f + buttonSpacing, buttonSpacing, 0);
		buttonAnalyze.transform.position = new Vector3(buttonLock.transform.position.x + Screen.width/4.8f + buttonSpacing, buttonSpacing, 0);

		buttonColorInfo.transform.position = new Vector3(aimImage.transform.position.x, aimImage.transform.position.y - 30f*(Screen.height/480f) - 20f, 0);

		
		
		
		// Initiate device camera and define the center
		
		WebCamDevice[] devices = WebCamTexture.devices;
		webcamTexture = new WebCamTexture(devices[0].name);
		Vector3 centerPos = new Vector3(Mathf.Round(Screen.width*0.5f), Screen.height*0.5f, 0);
		rawimage.transform.position = centerPos;
		aimImage.transform.position = rawimage.transform.position;
		ax = (int) rawimage.transform.position.x;
		ay = (int) rawimage.transform.position.y;
        rawimage.texture = webcamTexture;
        rawimage.material.mainTexture = webcamTexture;

        // Start displaying the camera feed on screen
        webcamTexture.Play();

       	// Rotate camera feed (mobile devices pull landscape camera feed and it needs to be rotated for usage in portrait mode)
       	rawimage.transform.rotation = Quaternion.AngleAxis(-90, Vector3.forward);
       
       	// Initiate 5x5 texture for sampling - for maximal optimization, we are not sampling whole screen
      	tex = new Texture2D(5, 5, TextureFormat.RGB24, false);      
      	
   	}
        
   	// Executes each frame
	void Update ()
	{
		// Sample RGB channels for every odd pixel in the grid with ax and ay in the center
		// We are sampling multiple points in the grid instead of a single pixel, in order to get the average color value
		// Single pixel color value can vary greatly, depending on the external conditions, condition of the camera lens, etc.
				

    	// Execute coroutine that collects new pixels at the end of each frame, not sooner
 		StartCoroutine(getTex());

 		// Reset RGB values
    	REDValue = GREENValue = BLUEValue = 0;

    	// Sample new values
		for (int sY = 0; sY<=sampleSize; sY+=2){
			for (int sX = 0; sX<=sampleSize; sX+=2){
    	    	currentPixel = tex.GetPixel(sX, sY);
				REDValue += currentPixel.r;
				GREENValue += currentPixel.g;
				BLUEValue += currentPixel.b;
			}
		}
		
		// Average color
		REDValue = REDValue/totalSampledPixels;
		GREENValue = GREENValue/totalSampledPixels;
		BLUEValue = BLUEValue/totalSampledPixels;
		convertedColor = new Color(REDValue, GREENValue, BLUEValue, 1f);
		colHSV = new Color(REDValue, GREENValue, BLUEValue, 1f);
		Color.RGBToHSV(convertedColor, out hh, out ss, out vv);

    	// HUE
    	float hueDetect = hh*360f;
		if (hueDetect<=360f) {colorReport = "Red";} 
		if (hueDetect<=345f) {colorReport = "Magenta";}
		if (hueDetect<=320f) {colorReport = "Pink";}  
		if (hueDetect<=290f) {colorReport = "Purple";}   
		if (hueDetect<=250f) {colorReport = "Blue";}
		if (hueDetect<=190f) {colorReport = "Cyan";}
		if (hueDetect<=170f) {colorReport = "Green";}
		if (hueDetect<=70f) {colorReport = "Yellow";}
		if (hueDetect<=35f){if (vv>=0.6f) {colorReport = "Orange";} else {colorReport = "Brown";}}
		if (hueDetect<=15f) {colorReport = "Red";}
		if (ss<=0.2f) {colorReport = "Grey";}

		// INTENSITY

		if (vv<=0.6f)
		{
			intensityDetect = "Dark";
		}
		else
		{
			if (ss<=0.5f && vv>=0.75f) 
			{
				intensityDetect = "Light";
			} 
			else 
			{
				intensityDetect = "";
			}
		}


		if (ss<=0.25f && vv>=0.75f)
		{
			colorReport="White";
			intensityDetect = "";
		}
		
		if ((vv<=0.35f && ss<=0.35f) || vv<=0.25f)
		{
			colorReport="Black";
			intensityDetect = "";
		}

    	buttonColorInfo.GetComponentInChildren<Text>().text = intensityDetect + " " + colorReport;
	}
	
	// Coroutine - executes at the end of each frame, so the sample texture can get latest pixels
	IEnumerator getTex()
	{
    	yield return new WaitForEndOfFrame();
    	if(!pausedMode)
    	{
    		tex.ReadPixels(new Rect(ax-2, ay-2, ax+2, ay+2), 0, 0);
    	}
	}


	// Info
	public void showAlertDialog()
    { 
    	
    	string[]    _buttons    = new string[] {
            "Ok",
            "Terms of Use"
        };
    	NPBinding.UI.ShowAlertDialogWithMultipleButtons("About Camera Chromatica", "This is a sample message. \n Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ornare dictum nibh, quis vehicula justo sodales at. Curabitur tincidunt sodales orci sit amet vulputate. Praesent cursus quam a urna feugiat volutpat. Proin ut ante id lacus pharetra hendrerit. Donec ac ante eu neque posuere scelerisque. Pellentesque quis pretium eros. Donec pellentesque, sem id pellentesque vestibulum, risus nunc dapibus nulla, eget sollicitudin justo sem sit amet lacus.", _buttons, onButtonPressed);
    }

    private void onButtonPressed(string _buttonPressed)
    {
        if (_buttonPressed == "Terms of Use")
        {
        	showTerms();
        }
    }

    public void showTerms()
    {
    	NPBinding.UI.ShowAlertDialogWithSingleButton("Terms of Use", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ornare dictum nibh, quis vehicula justo sodales at. Curabitur tincidunt sodales orci sit amet vulputate. Praesent cursus quam a urna feugiat volutpat. Proin ut ante id lacus pharetra hendrerit. Donec ac ante eu neque posuere scelerisque. Pellentesque quis pretium eros. Donec pellentesque, sem id pellentesque vestibulum, risus nunc dapibus nulla, eget sollicitudin justo sem sit amet lacus.", "Close", null);
    }

    public void pauseSwitch ()
    {
    	pausedMode = !pausedMode;
    	if (pausedMode)
    	{
    		GameObject.Find("ButtonLock").GetComponent<Image>().sprite = lock1Image; 
    		NPBinding.UI.ShowToast("Color picker locked, press again to unlock", eToastMessageLength.SHORT);
    	}
    	else
    	{
    		GameObject.Find("ButtonLock").GetComponent<Image>().sprite = lock2Image;
    		NPBinding.UI.ShowToast("Color picker unlocked", eToastMessageLength.SHORT);
    	}
    }

    public void copyHexValue()
    {
    	byte rByte = (byte)(REDValue * 256);
		byte gByte = (byte)(GREENValue * 256);
		byte bByte = (byte)(BLUEValue * 256);

		hexVal = "#" + rByte.ToString("X2") + gByte.ToString("X2") + bByte.ToString("X2");
		Debug.Log(hexVal);
  
    	UniPasteBoard.SetClipBoardString(hexVal);

    	NPBinding.UI.ShowToast("Color hex value " + hexVal + " copied to clipboard", eToastMessageLength.SHORT);
    }
    
    public void sliderSwitch()
    {

    	sliderVisible = !sliderVisible;
    	if (sliderVisible)
    	{
    		sliderSettings.interactable = true;
    		GameObject.Find("ButtonSettings").GetComponent<Image>().sprite = checkImage;
    		sliderSettings.transform.position = new Vector3(buttonSettings.transform.position.x + Screen.width/9.6f - buttonSpacing, buttonSettings.transform.position.y + Screen.width/4.8f + buttonSpacing, 0);
    	}
    	else
    	{
    		sliderSettings.interactable = false;
    		GameObject.Find("ButtonSettings").GetComponent<Image>().sprite = originalImage;
    		sliderSettings.transform.position = new Vector3(-500, 0, 1);
    	}
    }
    
    public void updateAim()
    {
    	sampleSize = 2f+Mathf.Floor(sliderSettings.value)*2f;
    	float aimSize = sampleSize/4f;
    	totalSampledPixels = Mathf.Pow((sampleSize*0.5f+1f), 2.0f);
    	aimImage.transform.localScale = new Vector3(aimSize, aimSize, 1);
    	buttonColorInfo.transform.position = new Vector3(aimImage.transform.position.x, aimImage.transform.position.y - 30f*(Screen.height/480f) - 20f*aimSize, 0);
    }
}
