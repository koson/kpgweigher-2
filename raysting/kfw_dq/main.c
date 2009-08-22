//#include <t89c51rd2.h>
#include "stc51.h"
#include "utili.h"
#include "lcd.h"
#include "key.h"
#include "stdio.h"
#include "eeprom.h"
RUNDATA rdata;
xdata char buf[20];
uchar key;

double lastrx = -1;
void analog_timer()	interrupt 1 using 1
{
	Key_Scan();
}

void State_Update();
void State_Display()
{
	EA = 0;
	if(rdata.StateId == PG_MSG_RZERO)
	{
		LCD_Cls();
		LCD_PrintHz16(20,  10, "正在扣除残余电阻...");
		rdata.StateId = PG_MAIN;
		EA = 1;
		return;
	}
	if(rdata.StateId == PG_MSG_VZERO)
	{
		LCD_Cls();
		LCD_PrintHz16(20,  10, "正在进行扣零操作...");
		rdata.StateId = PG_MAIN;
		EA = 1;
		return;
	}

	if(rdata.StateId == PG_MAIN){
		LCD_Cls();
		if(rdata.Current == 0)
			sprintf(buf,"Ix= 1XA");
		if(rdata.Current == 1)
			sprintf(buf,"Ix= 1X0.707A");
		if(rdata.Current == 2)
			sprintf(buf,"Ix= 1X1.414A");

		LCD_Print6X8(11,1,buf);

		if(rdata.Range == RANGE_20m)
			sprintf(buf," 20m");
		if(rdata.Range == RANGE_200m)
			sprintf(buf,"200m");
		if(rdata.Range == RANGE_2)
			sprintf(buf,"   2");
		if(rdata.Range == RANGE_20)
			sprintf(buf,"  20");
		if(rdata.Range == RANGE_200)
			sprintf(buf," 200");
		if(rdata.Range == RANGE_2k)
			sprintf(buf,"  2k");
		if(rdata.Range == RANGE_20k)
			sprintf(buf," 20k");
		if(rdata.Range == RANGE_200k)
			sprintf(buf,"200k");
		if(rdata.Range == RANGE_2M)
			sprintf(buf,"  2M");
		if(rdata.Range == RANGE_20M)
			sprintf(buf," 20M");

		LCD_Print6X8(120,1,buf);
//		LCD_PrintHz16(150,1,"Ω(量程)");

		sprintf(buf,"P= %.4f",rdata.Pvalue);
		LCD_Print6X8(11,54,buf);
//		LCD_PrintHz16(61,54,"Ω");

		sprintf(buf,"T= %.1f",rdata.Temp);
		LCD_Print6X8(120,54,buf);
		
		State_Update();
		EA = 1;
		return;
	}
	if(rdata.StateId == PG_MENU1){
		LCD_Cls();
		LCD_PrintHz16(2,  2, " 1.量程选择");
		LCD_PrintHz16(120,2, " 2.校准电阻");
		LCD_PrintHz16(2,  23," 3.执行校零");
		LCD_PrintHz16(120,23," 4.电阻率设定");
		LCD_PrintHz16(2,  46," 5.串口设定");
		LCD_PrintHz16(120,46," 6.使用帮助");
		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_RANGE){
		LCD_Cls();
		LCD_PrintHz16(2,  2,   "请选择新的量程:   单位Ω");
		LCD_Print6X8(2  , 25, " 0:20m");
		LCD_Print6X8(62 , 25, " 1:200m");
		LCD_Print6X8(122, 25, " 2:2");
		LCD_Print6X8(182 ,25, " 3:20");
		LCD_Print6X8(2 ,  36, " 4:200");
		LCD_Print6X8(62  ,36, " 5:2k");
		LCD_Print6X8(122 ,36, " 6:20k");
		LCD_Print6X8(182, 36, " 7:200k");
		LCD_Print6X8(2 ,  47, " 8:2M");
		LCD_Print6X8(62 , 47, " 9:20M");
		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_CALISET){
		LCD_Cls();
		LCD_PrintHz16(2,  2, "当前标准电阻值:");
		LCD_PrintHz16(2,22,  "   输入新阻值:");
		sprintf(buf,"%.4f",rdata.Rcali[rdata.Range]);
		LCD_Print8X16(120,10,buf);
		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_PSET_L){
		LCD_Cls();
		LCD_PrintHz16(2,  2, "当前导线长度:");
		LCD_PrintHz16(2,22,  "   输入新长度:");
		sprintf(buf,"%.4f",rdata.Plength);
		LCD_Print8X16(120,10,buf);

		State_Update();
		EA = 1;
		return;

	}

	if(rdata.StateId == PG_PSET_R){
		LCD_Cls();
		LCD_PrintHz16(2,  2, "当前圆截面半径:");
		LCD_PrintHz16(2,22,  "   输入新半径:");
		sprintf(buf,"%.4f",rdata.Pradius);
		LCD_Print8X16(120,10,buf);

		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_PSET_W){
		LCD_Cls();
		LCD_PrintHz16(2,  2, "当前方截面宽度:");
		LCD_PrintHz16(2,22,  "   输入新宽度:");
		sprintf(buf,"%.4f",rdata.Pwidth);
		LCD_Print8X16(120,10,buf);

		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_PSET_H){
		LCD_Cls();
		LCD_PrintHz16(2,  2, "当前方截面高度:");
		LCD_PrintHz16(2,22,  "   输入新高度:");
		sprintf(buf,"%.4f",rdata.Pheight);
		LCD_Print8X16(120,10,buf);

		State_Update();
		EA = 1;
		return;

	}

	if(rdata.StateId == PG_PSET){
		LCD_Cls();
		LCD_PrintHz16(2,   2," 电阻率参数:");
		LCD_PrintHz16(110, 2," 1.导线长度");
		LCD_PrintHz16(2,  22," 2.方截面宽度");
		LCD_PrintHz16(110,22," 3.方截面高度");
		LCD_PrintHz16(2,  42," 4.圆截面半径");
		LCD_PrintHz16(110,42," 5.不计算电阻率");
		State_Update();
		EA = 1;
		return;
	}
	if(rdata.StateId == PG_SET232){
		LCD_Cls();
		LCD_PrintHz16(12,   2, "波特率设定:");
		LCD_PrintHz16(12,   22," 1. 关闭串口");
		LCD_PrintHz16(12,   42," 2. 2400");
		LCD_PrintHz16(120,  42," 3. 9000");
		State_Update();
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_HELP){
		LCD_Cls();
		LCD_PrintHz16(40, 2,  "1.精度说明");
		LCD_PrintHz16(40, 20, "2.联系地址");
		LCD_PrintHz16(40, 40, "3.使用设置");
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_HELP_ADDR){
		LCD_Cls();
		LCD_PrintHz16(60, 2,  "兰斯汀仪表研究所");
		LCD_Print8X16(40, 20, "www.raysting.com.cn");
		LCD_Print8X16(40, 40, "raysting@126.com");
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_HELP_PREC){
		LCD_Cls();
		LCD_PrintHz16(60, 18, "产品描述");
		LCD_Print8X16(40, 40, "www.raysting.com.cn");
		EA = 1;
		return;

	}
	if(rdata.StateId == PG_HELP_SET){
		LCD_Cls();
		LCD_PrintHz16(60, 18,  "产品设置");
		LCD_Print8X16(40, 40, "www.raysting.com.cn");
		EA = 1;
		return;

	}
	EA = 1;
}
void State_Update()
{
	EA = 0;
	if(rdata.StateId == PG_MAIN){
		LCD_ClsBlock(10,20,10+24*8,52);
		sprintf(buf,"%.4f",rdata.Rx);
		sprintf(buf+8," ");
		LCD_Print24X32(10,18,buf);
		//LCD_Print8X16(205,34,"m");
		LCD_PrintHz16(216,34,"Ω");
	}
	if(rdata.StateId == PG_MENU1){

		if(rdata.pos_len == 0)
			LCD_Print6X8(2,   2,"*");
		else
			LCD_Print6X8(2,   2," ");
		if(rdata.pos_len == 1)
			LCD_Print6X8(120, 2,"*");
		else
			LCD_Print6X8(120, 2," ");
		if(rdata.pos_len == 2)
			LCD_Print6X8(2,  23,"*");
		else
		    LCD_Print6X8(2,  23," ");
		if(rdata.pos_len == 3)
			LCD_Print6X8(120,23,"*");
		else
			LCD_Print6X8(120,23," ");
		if(rdata.pos_len == 4)
			LCD_Print6X8(2,  46,"*");
		else
			LCD_Print6X8(2,  46," ");
		if(rdata.pos_len == 5)
			LCD_Print6X8(120,46,"*");
		else
			LCD_Print6X8(120,46," ");

	}
	if(rdata.StateId == PG_RANGE){
		if(rdata.pos_len == 0)
			LCD_Print6X8(2  , 25, "*");
		else
			LCD_Print6X8(2  , 25, " ");

		if(rdata.pos_len == 1)
			LCD_Print6X8(62  , 25, "*");
		else
			LCD_Print6X8(62  , 25, " ");
		if(rdata.pos_len == 2)
			LCD_Print6X8(122 , 25, "*");
		else
			LCD_Print6X8(122 , 25, " ");
		if(rdata.pos_len == 3)
			LCD_Print6X8(182, 25, "*");
		else
			LCD_Print6X8(182, 25, " ");
		if(rdata.pos_len == 4)
			LCD_Print6X8(2 ,36, "*");
		else
			LCD_Print6X8(2 ,36, " ");
		if(rdata.pos_len == 5)
			LCD_Print6X8(62 ,36, "*");
		else
			LCD_Print6X8(62 ,36, " ");
		if(rdata.pos_len == 6)
			LCD_Print6X8(122  , 36, "*");
		else
			LCD_Print6X8(122  , 36, " ");
		if(rdata.pos_len == 7)
			LCD_Print6X8(182 , 36, "*");
		else
			LCD_Print6X8(182 , 36, " ");
		if(rdata.pos_len == 8)
			LCD_Print6X8(2, 47, "*");
		else
			LCD_Print6X8(2, 47, " ");
		if(rdata.pos_len == 9)
			LCD_Print6X8(62 ,47, "*");
		else
			LCD_Print6X8(62 ,47, " ");


	}
	if(rdata.StateId == PG_CALISET){
		LCD_Print6X8(120,34,"        ");
		LCD_Print6X8(120,34,rdata.tempbuf);
	}
	if(rdata.StateId == PG_PSET_R){
		LCD_Print6X8(120,34,"        ");
		LCD_Print6X8(120,34,rdata.tempbuf);
	}
	if(rdata.StateId == PG_PSET_L){
		LCD_Print6X8(120,34,"        ");
		LCD_Print6X8(120,34,rdata.tempbuf);
	}

	if(rdata.StateId == PG_PSET_W){
		LCD_Print6X8(120,34,"        ");
		LCD_Print6X8(120,34,rdata.tempbuf);
	}
	if(rdata.StateId == PG_PSET_H){
		LCD_Print6X8(120,34,"        ");
		LCD_Print6X8(120,34,rdata.tempbuf);
	}
	if(rdata.StateId == PG_PSET){
		if(rdata.pos_len == 0)
			LCD_Print6X8(110,   2,"*");
		else
			LCD_Print6X8(110,   2," ");
		if(rdata.pos_len == 1)
			LCD_Print6X8(2,   22,"*");
		else
			LCD_Print6X8(2,   22," ");
		if(rdata.pos_len == 2)
			LCD_Print6X8(110,   22,"*");
		else
			LCD_Print6X8(110,  22," ");
		if(rdata.pos_len == 3)
			LCD_Print6X8(2,   42,"*");
		else
			LCD_Print6X8(2,   42," ");
		if(rdata.pos_len == 4)
			LCD_Print6X8(110,   42,"*");
		else
			LCD_Print6X8(110,   42," ");

			}
	if(rdata.StateId == PG_SET232){
		if(rdata.pos_len == 0)
			LCD_Print6X8(12,   22,"*");
		else
			LCD_Print6X8(12,   22," ");
		if(rdata.pos_len == 1)
			LCD_Print6X8(12,   42,"*");
		else
			LCD_Print6X8(12,   42," ");
		if(rdata.pos_len == 2)
			LCD_Print6X8(120,  42,"*");
		else
			LCD_Print6X8(120,  42," ");
	}
	EA = 1;
}



ulong ch1val,ch2val;
#define ONESEC	1282
sbit KTT=P3^7;
void main()
{
	uchar i,pos;
	double chs,chx;
	int j;
	KTT = 0;
	LCD_Init();
	Key_Init();
	State_Init();	

	//set serial port parameter (clock 11.0592M)
	//9600 baut rate 8 data non parity and 1 stop.
	SCON = 0x70;
	PCON = 0x00;

	//timer count
	TH1 = 0xfd;
	TH0 = 0xd8;
	TL0 = 0xf0;
	//use timer 1 to be serial
	//use timer 0 too
	TMOD = 0x22;
	TR1 = 1; //start timer 1
	TR0 = 1; //start timer 0
	ET1 = 0;
	ES = 1;
	PS = 1;
	EA = 1;
	ET0 = 1;
	
	sm_Init();

	key = KEY_INVALID;
	 while(1)
	 { 
		if(key != KEY_INVALID)
		{
			if((key == KEY_BTN1)||(key == KEY_BTN2)||(key == KEY_BTN3)||(key == KEY_BTN4))
			{
				if(key == KEY_BTN1) //auto range or not
				{
					rdata.Rauto = (rdata.Rauto == AUTO_OFF) ? AUTO_ON : AUTO_OFF;
					display_buttons(key,rdata.Rauto);
				}
				if(key == KEY_BTN2) //auto ktt or not
				{
					rdata.Rktt = (rdata.Rktt == KTT_OFF) ? KTT_ON : KTT_OFF;
					display_buttons(key,rdata.Rktt);
				}
				if(key == KEY_BTN3) //current change
				{
					display_buttons(key,1); //start to switch the current
					rdata.Current++;
				        if(rdata.Current > CURRENT_SQRT0P5)
						rdata.Current = CURRENT_1; 
					//TODO switch the current
					display_buttons(key,0);

				}
				if(key == KEY_BTN4) //remove zero
				{
					display_buttons(key,1); //start to remove zero
					rdata.R0 = rdata.Rx;
					display_buttons(key,0);
				}
				SaveToEEPROM();
				State_Display();
			}else{
		 		i = rdata.StateId;
				pos = rdata.pos_len;
				State_Change(key);
				
				if(i != rdata.StateId){ 
					State_Display();
				}else{
				    //if((pos != rdata.pos_len) || (key == KEY_UP) || (key == KEY_DN))
						State_Update();
				}
			}
			key = KEY_INVALID;
	  	}else{
			if(rdata.StateId != PG_MAIN) //配置页不处理数据
			         continue;	
			if(j > 0)
			{
				j--;
				continue;
			}
		}
	}	
}
