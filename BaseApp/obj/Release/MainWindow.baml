   M S B A M L   `   `   `  ���� H  DKinectCapture, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:KinectCaptureKinectCapture      KinectCapture.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -localclr-namespace:KinectCapture     d `Microsoft.Kinect.Wpf.Controls, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e355Custom(http://schemas.microsoft.com/kinect/2014  -     � Title$  Kinect One Capture��$��629.224��$
��1079�� � WindowStartupLocation$ CenterScreen=� � 
ResizeMode$ NoResize=� � Icon$ Images/Kinect.ico��.��� .+��� ���	#FFF4F4F5$
��Left=�$	��428��$��7,7,0,0q�
���   �$	��Top=�$	��518���� ���	#FFF4F4F5$
��Left=�$	��428��$��	539,7,0,0q�
���   �$	��Top=�$	��518���� -    ��Name$ ButtonOK��$��Connect��$
��Left=�$��
11,467,0,0q�$	��Top=�$��75���� -    ��Name$ label��$��Frames:��$
��Left=�$��
11,492,0,0q�$	��Top=� J� RenderTransformOrigin$ 0.474,-0.038&��� -   $ FramesLabel��$��0��$
��Left=�$��
77,492,0,0q�$	��Top=�$ -0.053,0.115&��� -   $ 
StopButton��$
��Stop��$
��Left=�$��171,467,0,0q�$	��Top=�$��75��{�.  �� -   $ RecordButton��$��Record��$
��Left=�$��
91,467,0,0q�$	��Top=�$��75��{�.  �� -   $ Export��$��Export��$
��Left=�$��421,521,0,0q�$	��Top=�$��75���� -   $ label1��$��Export Path:��$
��Left=�$��
11,518,0,0q�$	��Top=��� -	    ��Name$ 
ExportPath��$
��Left=�$��23��$��
89,519,0,0q� �� TextWrapping$ NoWrap=�$	��Top=�$	��300��	 �� AcceptsReturn	 .  �� -
   $ FolderButton��$	��...��$
��Left=�$��394,521,0,0q�$	��Top=�$��22��� -   
 �Name$
 ExportProgress��$
��Left=�$��7��$��
89,542,0,0q�$	��Top=�$	��300���� -    ��Name$ ColorImageViewer��$
��Left=�$��424                ��$��	10,10,0,0q�$	��Top=�$	��512���� -   $ DepthImageViewer��$
��Left=�$	��424��$��
542,10,0,0q�$	��Top=�$	��512���� -   $ 
label_Copy��$��Memory:��$
��Left=�$��542,463,0,0q�$	��Top=�$ 0.474,-0.038&�� -   $
 	MemoryBar��$
��Left=�$��26��$��604,463,0,0q�$	��Top=�$	��435���� -   $ MemoryLB��$��0 MBytes (0%)��$
��Left=�$��604,492,0,0q�$	��Top=�$ 0.474,-0.038&��� -   $ label_Copy1��$��Usage:��$
��Left=�$��554,519,0,0q�$	��Top=�$ 0.474,-0.038&�� -   $
 UsageBar��$
��Left=�$��26��$��604,519,0,0q�$	��Top=�$	��435���� -   $ UsageLB��$��0 MBytes (0%)��$
��Left=�$��604,548,0,0q�$	��Top=�$ 0.474,-0.038&��� -   $ ExportLB��$"��                            ��$
��Left=�$��91,554,0,-4q�$	��Top=�$ 0.474,-0.038&��� -   $ label2��$��Uncompressed Color��$
��Left=�$��	4,433,0,0q�$	��Top=�
��������� -   $ label3��$��Depth��$
��Left=�$��535,432,0,0q�$	��Top=�
��������� -   $ About��$��?��$
��Left=�$��501,467,0,0q�$	��Top=�$��24���� -   $ ClearButton��$��Clear��$
��Left=�$��251,467,0,0q�$	��Top=�$��75��