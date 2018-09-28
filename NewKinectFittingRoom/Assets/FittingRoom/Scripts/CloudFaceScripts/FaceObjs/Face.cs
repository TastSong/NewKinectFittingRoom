using System;

[Serializable]
public class FacesCollection
{
	public Face[] faces;
}

[Serializable]
public class Face
{

	public string faceId;

	public FaceRectangle faceRectangle;

	public FaceLandmarks faceLandmarks;

	public FaceAttributes faceAttributes;

}
