#include "utili.h"
#include "stc51.h"
sbit SPI_CK = P3^5;
sbit SPI_DI1 = P3^4;
sbit SPI_DI2 = P0^5;
sbit SPI_DO = P3^3;

/*---------------延时子程序----------------*/
void delay (uint us) 
{
  	while(us--);
}
void delay1 (uint ms) 
{
  	int i,j;
  	for(i=0;i<ms;i++)
  	for(j=0;j<1000;j++)
  		;
		\
}
//计算字符串长度
uchar strlen(uchar *s)
{
	uchar len = 0;
	while(*s++) len ++;
	
	return len;
}
double buf2double()		
{
	double tmp = 0.0;
	uchar i = 0;
	uchar pos = 0;
	for(i=0;i<rdata.pos_len;i++)
	{
		if(rdata.tempbuf[i] != KEY_DOT)
			tmp = tmp * 10.0+(rdata.tempbuf[i] - '0');
		else
			pos = rdata.pos_len - i - 1;
	}
	while(pos > 0)
	{
		tmp = tmp / 10.0;
		pos--;
	}
	return tmp;
}
int buf2byte()	    //convert rdata.tempbuf to byte (00-99)
{
	int tmp = 0;
	uchar i;
	for(i=0;i<rdata.pos_len;i++)
	{
		tmp = tmp * 10+(rdata.tempbuf[i] - '0');
	}
	return tmp;
}

//MY SPI Related function 
uchar xdata ch1buf[5];
uchar xdata ch2buf[5];
#define SM_WDELAY	100
#define SM_RDELAY	100
#define SM_RDELAY2	10
#define HEAD_MARK	0xcc	//header mark
void sm_Init()
{	
	uchar i;
	i = SPI_DI1; //set to input
	i = SPI_DI2; //set to input
}
void   sm_write(uchar   value)  
{  
	uchar   no;  
       
  	for(no=0;no<8;no++) {  
	       SPI_CK = 1;      
		 
   	  	if   ((value &0x80)==0x80)  
       		  SPI_DO = 1;  
		else  
		      SPI_DO = 0;  
		 delay(SM_WDELAY);   
	     SPI_CK = 0;    
   		 value   =   (value <<1);  
		delay(SM_WDELAY);
  	}
	SPI_CK = 1;
}  
void sm_read(uchar pos,uchar ch)
{  
   	uchar   no,value1,value2;  
   
 	for (no=0;no<8;no++) 	{ 
		  SPI_CK = 1;  
		  delay(SM_RDELAY);		
		  if( ch & HASCH1)
		          value1   =   (value1   <<1);  
		  if( ch & HASCH2)
			  value2   =   (value2   <<1);  
         	  SPI_CK = 0;  
		  delay(SM_RDELAY2);
		  if( ch & HASCH1){
	 		  if (SPI_DI1 == 1)  
		        	value1  |=0x01;  
  			  else  
				value1  &=~0x01;  
		  }
		  if( ch & HASCH2) {
	 		  if (SPI_DI2 == 1)  
			        value2  |=0x01;  
  			  else  
				value2  &=~0x01;  
		  }
	}
	SPI_CK = 1;
	if( ch & HASCH1)
		ch1buf[pos] = value1;
	if( ch & HASCH2)
		ch2buf[pos] = value2;
 }  

uchar collect_long(uchar cmd,uchar ch)
{
	sm_write(HEAD_MARK);
	sm_write(cmd);
	sm_read(0,ch);
	sm_read(1,ch);
	sm_read(2,ch);
	sm_read(3,ch);
	sm_read(4,ch);
			
	if((~ch1buf[4] == (ch1buf[0]+ch1buf[1]+ch1buf[2]+ch1buf[3])) && (ch & HASCH1))
	{
		ch1val = 0;
		ch1val = ch1val + ch1buf[0];	ch1val <<= 8;
		ch1val = ch1val + ch1buf[1];	ch1val <<= 8;
		ch1val = ch1val + ch1buf[2];	ch1val <<= 8;
		ch1val = ch1val + ch1buf[3];	ch1val <<= 8;
	}else{
		return 0;
	}
	if((~ch2buf[4] == (ch2buf[0]+ch2buf[1]+ch2buf[2]+ch2buf[3])) && (ch & HASCH2))
	{
		ch2val = 0;
		ch2val = ch2val + ch2buf[0];	ch2val <<= 8;
		ch2val = ch2val + ch2buf[1];	ch2val <<= 8;
		ch2val = ch2val + ch2buf[2];	ch2val <<= 8;
		ch2val = ch2val + ch2buf[3];	ch2val <<= 8;
	
	}else{
		return 0;
	}
	return 1;

}


