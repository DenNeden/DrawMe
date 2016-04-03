using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.UI;

public class Touch : MonoBehaviour {
	public GameObject levelRenderer;
	public GameObject touchPic;
	public GameObject curPoint;
	public GameObject levelPoint;
	public GameObject levelEditor;
	public Text editorCountText;
	public GameObject savedText;
	public int level = 0;
	int editorCount = 0;
	GameObject[] curPoints;
	GameObject[] levelPoints;
	Vector3 curPos;
	Vector3 prevCurPos;
	Vector3 prevPrevCurPos;
	Vector3 lastPoint;
	float maxPrevDist = 0.1f;
	float maxAngle = 30f;
	float minPointDist = 1f;
	float lastToFirstMinDist = 1f;
	float compareDist = 1f;
	string levelsFileName;
	bool newTouch = true;
	bool editorHasShape = false;
	bool levelIsLoaded = false;
	XDocument xdoc;
	XElement levels;
	LineRenderer curPointsLiner;
	LineRenderer levelLiner;
	// Use this for initialization
	void Start () {
		levelsFileName = Application.dataPath+"/lvls.xml";
		touchPic.SetActive (false);
		curPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
		prevCurPos = curPos;
		prevPrevCurPos = prevCurPos;
		OpenXDoc ();
		curPointsLiner = GetComponent<LineRenderer> ();
		levelLiner = levelRenderer.GetComponent<LineRenderer> ();
	}
	
	// Update is called once per frame
	void Update () {
		curPoints = GameObject.FindGameObjectsWithTag ("CurPoint");
		if (Input.GetMouseButtonDown(0))
		if (!levelEditor.activeInHierarchy) curPointsLiner.SetVertexCount (0);

		if (Input.GetMouseButton (0) && newTouch && !editorHasShape) {
			curPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			curPos.z = 0;
			touchPic.transform.position = curPos;
			if (!touchPic.activeInHierarchy) {
				AddCurPoint (curPos);
			}

			float curDist = Vector3.Distance (prevCurPos, curPos);
			if (curDist > maxPrevDist) {
				if (prevPrevCurPos != prevCurPos) {
					Vector3 prevVect = prevPrevCurPos - prevCurPos;
					Vector3 curVect = prevCurPos - curPos;
					float angle = Vector3.Angle (prevVect, curVect);
					float lastPointDist = Vector3.Distance (lastPoint, prevCurPos);
					if (angle > maxAngle && lastPointDist > minPointDist) {
						AddCurPoint (prevCurPos);
					}
				}
				prevPrevCurPos = prevCurPos;
				prevCurPos = curPos;
			}
			if (curPoints.Length > 2 && Vector3.Distance (prevCurPos, curPoints [0].transform.position) < lastToFirstMinDist) {
				CloseShape ();
			}
			touchPic.SetActive (true);
		} else
			ClearTouch ();

		if (Input.GetMouseButtonUp (0)) {
			newTouch = true;
			ClearTouch ();
			if (!levelEditor.activeInHierarchy) ClearCurPoints ();
		}
	} // End of Update		

	void LateUpdate () {
		if (!levelIsLoaded && !levelEditor.activeInHierarchy)
			LoadLevel (level);
	} // End of LateUpdate

	void ClearTouch () {
		if (touchPic.activeInHierarchy) touchPic.GetComponent<TrailRenderer> ().Clear ();
		touchPic.SetActive (false);
	} // end of ClearTouch

	public void ClearCurPoints () {
		for (int i = 0; i < curPoints.Length; i++) {
			GameObject.Destroy (curPoints [i]);
			editorHasShape = false;
			if (levelEditor.activeInHierarchy) curPointsLiner.SetVertexCount (0);
		}
	} // End of ClearCurPoints

	public void ClearLevelPoints () {
		for (int i = 0; i < levelPoints.Length; i++) {
			GameObject.Destroy (levelPoints [i]);
		}
		levelLiner.SetVertexCount (0);
		curPointsLiner.SetVertexCount (0);
		levelIsLoaded = false;
	} // End of ClearLevelPoints

	void AddCurPoint (Vector3 point) {		
		Instantiate (curPoint, point, Quaternion.identity);
		prevCurPos = point;
		prevPrevCurPos = point;
		lastPoint = point;
	} // end of AddCurPoints

	void CloseShape () {
		ResizeShape (curPoints);
		DrawLines (curPoints, curPointsLiner);
		ClearTouch ();
		newTouch = false;
		editorHasShape = levelEditor.activeInHierarchy;
		if (!levelEditor.activeInHierarchy) {
			if (Compare ())
				GetComponent<Starter> ().WinLevel ();
		}
	} // End of CloseShape

	void ResizeShape (GameObject[] points){
		int length = points.Length;
		int upperPoint = 0;
		int lowerPoint = 0;
		int leftPoint = 0;
		int rightPoint = 0;
		//left right upper lower points
		for (int i =0; i < length; i++){
			if (points [i].transform.position.y > points [upperPoint].transform.position.y)
				upperPoint = i;
			if (points [i].transform.position.y < points [lowerPoint].transform.position.y)
				lowerPoint = i;
			if (points [i].transform.position.x < points [leftPoint].transform.position.x)
				leftPoint = i;
			if (points [i].transform.position.x > points [rightPoint].transform.position.x)
				rightPoint = i;
		}
		//End of left right upper lower points

		//Multiplier
		float height = points[upperPoint].transform.position.y - points[lowerPoint].transform.position.y;
		float width = points[rightPoint].transform.position.x - points[leftPoint].transform.position.x;
		float maxHeight = Camera.main.ScreenToWorldPoint (new Vector2(0, Camera.main.pixelHeight)).y;
		float maxWidth = Camera.main.ScreenToWorldPoint (new Vector2(Camera.main.pixelWidth, 0)).x;
		float heightMult = maxHeight / height;
		float widthMult = maxWidth / width;
		float minMult;
		if (heightMult < widthMult) {
			minMult = heightMult;
		} else {
			minMult = widthMult;
		}
		//End of Multiplier

		//move left/down
		float xShift = points [leftPoint].transform.position.x;
		float yShift = points [lowerPoint].transform.position.y;
		for (int i = 0; i < length; i++) {
			points [i].transform.position = new Vector3 (points [i].transform.position.x - xShift, points [i].transform.position.y - yShift, 0);
		}
		//end of move left/down

		// zoom
		for (int i = 0; i < length; i++) {
			points [i].transform.position = new Vector3 (points [i].transform.position.x * minMult, points [i].transform.position.y * minMult, 0);
		}
		// end of zoom

		//center
		for (int i = 0; i < length; i++) {
			points [i].transform.position = new Vector3 (points [i].transform.position.x - width * minMult/2, points [i].transform.position.y - height * minMult/2, 0);
		}
		//end of center

	} // End of ResizeShape

	void DrawLines (GameObject[] points, LineRenderer rend) {
		rend.SetVertexCount (points.Length+1);
		for (int i = 0; i < points.Length; i++) {
			rend.SetPosition (i, points [i].transform.position);
		}
		rend.SetPosition (points.Length, points [0].transform.position);
	}

	public void AddLevel () {
		GameObject[] points = curPoints;
		XElement level = new XElement ("level");
		int i = 0;	
		foreach (GameObject go in points) {
			XElement point = new XElement ("point");
			point.Add (new XAttribute ("id", i++));
			point.Add (new XAttribute ("x", go.transform.position.x));
			point.Add (new XAttribute ("y", go.transform.position.y));
			level.Add (point);
		}
		levels.Add (level);
		editorCount++;
		editorCountText.text = ""+editorCount;
	} //end of AddLevel

	void OpenXDoc () {
		xdoc = new XDocument ();
		levels = new XElement ("levels");
	} // end of OpenXDoc

	public void SaveLevels () {
		xdoc.Add (levels);
		xdoc.Save (levelsFileName);
	} // End of SaveLevels

	public void LoadLevel (int curLevel) {
			XDocument ldoc = XDocument.Load (levelsFileName);
			int li = 0;
			foreach (XElement lvl in ldoc.Root.Elements("level")) {
				if (li == curLevel)
					foreach (XElement point in lvl.Elements()) {
						//int pointNumber = int.Parse (point.Attribute ("id").Value);
						Vector3 pos;
						pos.x = float.Parse (point.Attribute ("x").Value);
						pos.y = float.Parse (point.Attribute ("y").Value);
						pos.z = 0;
						Instantiate (levelPoint, pos, Quaternion.identity);
					}
				li++;
			}
			levelPoints = GameObject.FindGameObjectsWithTag ("LevelPoint");
			ResizeShape (levelPoints);
			levelLiner.SetVertexCount (0);
			DrawLines (levelPoints, levelLiner);
			levelIsLoaded = true;
	
	} // End of LoadLevel

	bool Compare (){
		int [] curP = new int[curPoints.Length] ;
		int [] levP = new int[levelPoints.Length] ;
		int foundPoints = 0;
		int sameDirPoints = 0;
		if (curPoints.Length == levelPoints.Length) {
//			bool round1 = false;
//			bool round2 = false;

			for (int i = 0; i < curPoints.Length; i++) {
				for (int v = 0; v < levelPoints.Length; v++) {
					if (Vector3.Distance (curPoints [i].transform.position, levelPoints [v].transform.position) < compareDist) {
						foundPoints++;
						curP[i] = i;
						levP[i] = v;
					}
				}
			}
			//print ("foundpoints: " + foundPoints);
//			for (int i = 0; i < curP.Length; i++) {
//				print ("curP: " + curP[i]);
//				print ("levP: " + levP[i]);
//			}
			int shift = levP [0];
			for (int i = 0; i < levP.Length; i++) {
				int k = i+shift;
				if (shift + i >= levP.Length)
					k = shift + i - levP.Length;
				
				if (levP [i] == k)
					sameDirPoints++;
			}
			if (sameDirPoints == levelPoints.Length) {
			//	print ("firstpass-samedir: " + sameDirPoints);
				return true;
			}

			sameDirPoints = 0;
			//print ("try second pass");
			for (int i = 0; i < levP.Length; i++) {
				int k = shift-i;
				if (shift - i < 0)
					k = shift - i + levP.Length;

				if (levP [i] == k)
					sameDirPoints++;
			}

			if (sameDirPoints == levelPoints.Length) {
				//print ("secondpass-samedir: " + sameDirPoints);
				return true;
			}
			return false;
		} else
			return false; // lenght !=
	} // end of Compare
} // End of Touch class
