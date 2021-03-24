Temat pracy: Aplikacja oparta o architekturę klient-serwer do archiwizacji pracy studenta oparta o technologię .Net.


Autor: Kamil Paluch


Stack technologiczny:
- .NET Core 3.1;
- Windows Presentation Foundation;


Opis Projektu:
Oprogramowanie z architekturą klient-serwer archiwizujące pracę studenta w obrębie jednej sali laboratoryjnej. Serwer obsługuje wielu klientów w jednym czasie, wykorzystując do tego wielowątkowość. Aplikacja serwera zarządza operacjami możliwymi do wykonania poprzez poszczególne tryby pracy. Klient po nawiązaniu połączenia z serwerem ma możliwość odbierania materiałów od prowadzącego oraz wysyłania swoich autorskich rozwiązań.  


Cel projektu:
Stworzenie oprogramowania, które usprawni prowadzącym wykonywanie laboratoriów odbywających się w salach komputerowych, poprzez łatwiejsze udostępnianie materiałów dydaktycznych. Projekt również zwiększy wygodę oraz przyspieszy proces przeprowadzania testów sprawdzających wiedzę studenta, wykorzystując prostszą i efektywniejszą metodę przesyłania rozwiązań opracowanych przez uczestników testu.


Założenia projektu:
- Rozproszenie aplikacji na część serwerową i kliencką wraz z komunikacją opartą na Socketach z wykorzystaniem protokołu TCP/IP.
- Serwer nasłuchuje w poszukiwaniu aktywnych klientów w wątku pracującym w tle. Obsługa komunikacji w osobnym wątku (1 połączony klient = 1 wątek).
- Transmisja plików z wykorzystaniem archiwów ZIP. Aplikacje z możliwością utworzenia archiwów wraz z możliwością zabezpieczenia ich hasłem. 
- Klient po podłączeniu automatycznie odbiera i zapisuje pliki we wcześniej zdefiniowanym miejscu na dysku.
- Stworzenie interfejsu przyjaznego użytkownikowi. Aplikacja kliencka uproszczona względem aplikacji serwera. 
- Zabezpieczenie wielowątkowości wykorzystując metody synchronizacji pracy w sekcjach krytycznych. 
