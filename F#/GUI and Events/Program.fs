﻿(******************************
           Лозов Пётр
           Группа 271
            18.10.13
         Красивый курсор
 *******************************)

open System
open System.Windows.Forms
open System.Drawing

type BeautifulCursor(container : ContainerControl, size : float32, length : int) =
    
    // цвет будущей линии 
    let mutable color = (0, 0, 0)

    // текущие координаты курсора
    let mutable pointCursor  =
        new PointF(float32 <| container.Width / 2, float32 <| container.Height / 2)
    
    // последовательность из length отрисовываемых линий
    let mutable seqLines =
        let startLine = (pointCursor, pointCursor), color
        seq {for i in 1..length -> startLine}
  
    // максимальная толщина курсора
    let size = 
        if size > 0.0f then size else failwith "Invalid value Size."
   
    // количество линий в курсоре
    let length = 
        if length > 0 then length else failwith "Invalid value Length."
        
    do
        // создаём таймер, срабатывающий каждые 10 мс
        let timer = new Timers.Timer(
                                        Interval = 10.0, 
                                        Enabled  = true                              
                                    )

        // создаём событие, срабатывающее от таймера, аргументами
        // которого является всё необходимое для отрисовки линии 
        // (координаты двух точек и три числа, определяющие цвет)   
        let eventTimer = timer.Elapsed
                      |> Event.map (fun _ -> pointCursor) 
                      |> Event.pairwise
                      |> Event.map (fun x -> (x, color))

        // при срабатывании события event обновляем последовательность
        // линий, генерируем следующий цвет и вызываем перерисовку
        // (срабатывает событие container.Paint)
        eventTimer.Add(
                  fun x ->
                      // генерация нового цвета использует код Грея для плавного
                      // перехода от одного цвета к другому
                      let nextColor color =
                          match color with
                          | ( 0 , 0 , b ) when b < 255 -> ( 0 , 0 ,b+5)
                          | ( 0 , g ,255) when g < 255 -> ( 0 ,g+5,255)
                          | ( 0 ,255, b ) when b >  0  -> ( 0 ,255,b-5)
                          | ( r ,255, 0 ) when r < 255 -> (r+5,255, 0 )
                          | (255,255, b ) when b < 255 -> (255,255,b+5)
                          | (255, g ,255) when g >  0  -> (255,g-5,255)
                          | (255, 0 , b ) when b >  0  -> (255, 0 ,b-5)
                          | ( r , 0 , 0 ) when r >  0  -> (r-5, 0 , 0 )
                          | _ -> color

                      // обновление последовательности линий
                      seqLines <- Seq.truncate length <| Seq.append [x] seqLines
                      // генерация нового цвета
                      color <- nextColor color
                      // вызов перересовки
                      container.Invalidate()
                 )
    
        // как только курсор попадает внутрь container, скрываем системный курсор
        container.MouseEnter.Add(fun _ -> Cursor.Hide())
        // как только курсор выходит за пределы внутренней части container, показываем системный курсор
        container.MouseLeave.Add(fun _ -> Cursor.Show())
        // как только у курсора изменилось положение на container, записываем их в pointCursor
        container.MouseMove.Add(fun p -> pointCursor <- new PointF(float32 p.X, float32 p.Y))

        // отрисовка курсора
        container.Paint.Add(
                                fun x -> 
                                    // рисуем линию заданного цвета и толщины, а также закрашенную
                                    // окружность  того же цвета с диаметром равным толщине линии,
                                    // чтобы скрыть стыки линий
                                    let draw ((p1:PointF,p2:PointF),(r,g,b)) i =
                                        let color = Color.FromArgb(r,g,b)
                                        use pen = new Pen(color, i)
                                        use brush = new SolidBrush(color)
                                        x.Graphics.FillEllipse(brush, p1.X - i / 2.0f, p1.Y - i / 2.0f, i, i)
                                        x.Graphics.DrawLine(pen, p1, p2)

                                    // включаем сглаживание отрисовки
                                    x.Graphics.SmoothingMode <- Drawing2D.SmoothingMode.HighQuality
                                    // последовательно, начиная с хвоста, отрисовываем курсор,
                                    // постепенно увеличивая его толщину
                                    Seq.iter2 draw (List.rev <| List.ofSeq seqLines) 
                                        <| List.map (fun x -> float32 x * size / float32 length) [1..length]
                           )
              
type myForm() as this =
        
    inherit Form(
                    Text = "GUI and Events",
                    MaximizeBox = false,
                    MinimizeBox = false,
                    FormBorderStyle = FormBorderStyle.Fixed3D,
                    Height = 700,
                    Width = 700,
                    BackColor = Color.WhiteSmoke                                    
                )

    // увеличиваем буфер для отрисовки (чтоб не мограго)
    do this.DoubleBuffered <- true
    
Application.Run(
                    let form = new myForm()
                    let cursor = new BeautifulCursor(form, 20.0f, 20)
                    form
               )