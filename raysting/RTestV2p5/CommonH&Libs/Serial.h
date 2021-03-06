// Serial.h 
//the file is copied from vc++ bible,
//changes made are that data from char to unsigned char
//8bit,no parity,9600 baut,2 stopbit is the default connect


#ifndef __SERIAL_H__
#define __SERIAL_H__
#include "StdAfx.h"

#define FC_DTRDSR       0x01
#define FC_RTSCTS       0x02
#define FC_XONXOFF      0x04
#define ASCII_BEL       0x07
#define ASCII_BS        0x08
#define ASCII_LF        0x0A
#define ASCII_CR        0x0D
#define ASCII_XON       0x11
#define ASCII_XOFF      0x13

class CSerial
{
public:
	
	CSerial(BYTE nport = 0);
	~CSerial();
	int SendData(int portnum,char *buffer,int size);
	BOOL Open( BYTE nPort, DWORD nBaud,BYTE nStopBits,BYTE nParity,BYTE nDataBits);

	BOOL Close( void );

	int ReadData( void *, int );

	int SendData( int portnum,unsigned char *buffer, int size);

	int ReadDataWaiting( void );

	BOOL IsOpened( void );

	void SetPort(unsigned char nPort){m_spid = nPort;};

protected:

	BOOL WriteCommByte( unsigned char );

	HANDLE m_hIDComDev;
	OVERLAPPED m_OverlappedRead,m_OverlappedWrite;
	BOOL m_bOpened;
	unsigned char m_spid;
};
#endif

