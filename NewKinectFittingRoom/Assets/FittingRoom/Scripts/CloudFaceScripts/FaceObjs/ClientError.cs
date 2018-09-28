using System;
using System.Net;
using System.Runtime.Serialization;


[Serializable]
public class ClientError
{

	public ClientExceptionMessage error;

}


[Serializable]
public class ClientExceptionMessage
{

	public string code;

	public string message;

}

[Serializable]
public class ServiceError
{
	public string statusCode;

	public string message;

}

