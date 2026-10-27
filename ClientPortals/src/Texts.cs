using System.Collections.Generic;

namespace ClientPortals;

/// <summary>
/// Every text the mod shows, in every language the game ships (its localization file's columns), in the language of
/// the <see cref="Plugin.Language"/> setting or, on Auto, the game's. A language without a row (Abenaki) gets English.
/// <c>$1</c> marks where a value goes.
/// </summary>
public static class Texts
{
    /// <summary>The game's languages (its localization file's columns), the choices of the Language setting.</summary>
    public static readonly string[] GameLanguages =
    {
        "English", "Swedish", "French", "Italian", "German", "Spanish", "Russian", "Romanian", "Bulgarian",
        "Macedonian", "Finnish", "Danish", "Norwegian", "Icelandic", "Turkish", "Lithuanian", "Czech",
        "Hungarian", "Slovak", "Polish", "Dutch", "Portuguese_European", "Portuguese_Brazilian", "Chinese",
        "Chinese_Trad", "Japanese", "Korean", "Hindi", "Thai", "Abenaki", "Croatian", "Georgian", "Greek",
        "Serbian", "Ukrainian", "Latvian",
    };

    public const string Auto = "Auto";

    // the order of the texts in each row below
    public static readonly string[] Keys =
    {
        "title", "from", "search", "tab_all", "tab_favorites", "sort_name", "map_view", "list_view", "close", "known",
        "hidden", "learn_more", "travel", "favorite", "tabs", "select", "next", "cancel", "travel_to", "unnamed",
        "empty_none", "empty_hidden", "empty_favorites", "empty_search", "new", "gone", "missing", "table_learned", "table_recorded", "table_count",
        "map_hint", "click", "right_click", "sort_nearest", "sort_farthest", "tab_ignored", "ignore", "empty_ignored", "ignored", "rename_both",
        "rename_failed", "rename", "rename_unreachable", "only_portals", "middle_click", "ignored_unnamed",
        "portal_names",
    };

    public static readonly Dictionary<string, string[]> Rows = new()
    {
        ["English"] = new[]
        {
            "Portals", "From: $1", "Search...", "All", "Favorites", "Sort: name", "Map view", "List view", "Close", "Known portals: $1",
            "Hidden nearby: $1", "Learn more by exploring or at a cartography table", "Travel", "Favorite", "Tabs", "Select", "Next portal", "Cancel", "Travel to...", "Unnamed portal",
            "You don't know any other portal yet. Visit portals or read a cartography table.", "All the portals you know are nearby (hidden).", "No favorite portals yet.", "No portal matches the search.", "New portal: $1", "Portal $1 is gone. Removed from your list.", "There is no portal here anymore. Removed from your list.", "Portals learned from the table: $1", "New portals recorded on the table: $1", "Portals recorded: $1",
            "Choose a portal on the map", "Click", "Right-click", "Sort: nearest", "Sort: farthest", "Ignored", "Ignore", "No ignored portals.", "$1 is ignored. It is on the Ignored tab.", "Rename both ends",
            "The other portal is out of reach: only this one was renamed.",
            "Rename", "This portal is out of reach: visit it to rename it.", "Only portals", "Middle-click", "Unnamed portals are ignored. They are on the Ignored tab.",
            "Portal names",
        },
        ["Russian"] = new[]
        {
            "Порталы", "Отсюда: $1", "Поиск...", "Все", "Избранное", "Сортировка: имя", "Карта", "Список", "Закрыть", "Известно порталов: $1",
            "Скрыто рядом: $1", "Узнавайте новые, исследуя мир или у картографического стола", "Перейти", "В избранное", "Вкладки", "Выбор", "Следующий портал", "Отмена", "Переместиться в...", "Портал без имени",
            "Вы пока не знаете других порталов. Посетите порталы или изучите картографический стол.", "Все известные вам порталы находятся рядом (скрыты).", "Избранных порталов пока нет.", "Нет порталов, подходящих под поиск.", "Новый портал: $1", "Портал $1 исчез. Удалён из списка.", "Здесь больше нет портала. Удалён из списка.", "Порталов узнано со стола: $1", "Новых порталов записано на стол: $1", "Записано порталов: $1",
            "Выберите портал на карте", "Клик", "Правый клик", "Сортировка: ближние", "Сортировка: дальние", "Скрытые", "Скрыть", "Скрытых порталов нет.", "$1 скрыт. Он на вкладке «Скрытые».", "Переименовать пару",
            "Второй портал вне досягаемости: переименован только этот.",
            "Переименовать", "Портал вне досягаемости: посетите его, чтобы переименовать.", "Только порталы", "Средний клик", "Порталы без имени скрыты. Они на вкладке «Скрытые».",
            "Названия порталов",
        },
        ["Ukrainian"] = new[]
        {
            "Портали", "Звідси: $1", "Пошук...", "Усі", "Обране", "Сортування: назва", "Мапа", "Список", "Закрити", "Відомо порталів: $1",
            "Приховано поруч: $1", "Дізнавайтеся більше, досліджуючи світ або біля картографічного столу", "Перейти", "До обраного", "Вкладки", "Вибір", "Наступний портал", "Скасувати", "Переміститися до...", "Портал без назви",
            "Ви ще не знаєте інших порталів. Відвідайте портали або прочитайте картографічний стіл.", "Усі відомі вам портали поруч (приховані).", "Обраних порталів поки немає.", "Жоден портал не відповідає пошуку.", "Новий портал: $1", "Портал $1 зник. Вилучено зі списку.", "Тут більше немає порталу. Вилучено зі списку.", "Порталів дізнано зі столу: $1", "Нових порталів записано на стіл: $1", "Записано порталів: $1",
            "Виберіть портал на мапі", "Клік", "Правий клік", "Сортування: ближчі", "Сортування: дальші", "Приховані", "Приховати", "Прихованих порталів немає.", "$1 приховано. Він на вкладці «Приховані».", "Перейменувати пару",
            "Другий портал поза досяжністю: перейменовано лише цей.",
            "Перейменувати", "Портал поза досяжністю: відвідайте його, щоб перейменувати.", "Лише портали", "Середній клік", "Портали без назви приховано. Вони на вкладці «Приховані».",
            "Назви порталів",
        },
        ["Bulgarian"] = new[]
        {
            "Портали", "Оттук: $1", "Търсене...", "Всички", "Любими", "Подреждане: име", "Карта", "Списък", "Затвори", "Известни портали: $1",
            "Скрити наблизо: $1", "Откривайте още, като изследвате или на картографската маса", "Пътувай", "Любим", "Раздели", "Избор", "Следващ портал", "Отказ", "Пътувай до...", "Портал без име",
            "Все още не познавате други портали. Посетете портали или прочетете картографска маса.", "Всички портали, които познавате, са наблизо (скрити).", "Все още няма любими портали.", "Няма портал, отговарящ на търсенето.", "Нов портал: $1", "Порталът $1 е изчезнал. Премахнат от списъка.", "Тук вече няма портал. Премахнат от списъка.", "Научени портали от масата: $1", "Нови портали, записани на масата: $1", "Записани портали: $1",
            "Изберете портал на картата", "Клик", "Десен клик", "Подреждане: най-близки", "Подреждане: най-далечни", "Игнорирани", "Игнорирай", "Няма игнорирани портали.", "$1 е игнориран. Ще го намерите в раздела „Игнорирани“.", "Преименувай двойката",
            "Другият портал е недостъпен: преименуван е само този.",
            "Преименувай", "Порталът е недостъпен: посетете го, за да го преименувате.", "Само портали", "Среден клик", "Порталите без име са игнорирани. Ще ги намерите в раздела „Игнорирани“.",
            "Имена на порталите",
        },
        ["Macedonian"] = new[]
        {
            "Портали", "Од тука: $1", "Пребарување...", "Сите", "Омилени", "Подредување: име", "Мапа", "Список", "Затвори", "Познати портали: $1",
            "Скриени во близина: $1", "Откријте повеќе со истражување или на картографската маса", "Патувај", "Омилен", "Јазичиња", "Избор", "Следен портал", "Откажи", "Патувај до...", "Портал без име",
            "Сè уште не знаете друг портал. Посетете портали или прочитајте картографска маса.", "Сите портали што ги знаете се во близина (скриени).", "Сè уште нема омилени портали.", "Ниту еден портал не одговара на пребарувањето.", "Нов портал: $1", "Порталот $1 исчезна. Отстранет од списокот.", "Тука веќе нема портал. Отстранет од списокот.", "Научени портали од масата: $1", "Нови портали запишани на масата: $1", "Запишани портали: $1",
            "Изберете портал на мапата", "Клик", "Десен клик", "Подредување: најблиски", "Подредување: најдалечни", "Игнорирани", "Игнорирај", "Нема игнорирани портали.", "$1 е игнориран. Ќе го најдете во јазичето „Игнорирани“.", "Преименувај го парот",
            "Другиот портал е недостапен: преименуван е само овој.",
            "Преименувај", "Порталот е недостапен: посетете го за да го преименувате.", "Само портали", "Среден клик", "Порталите без име се игнорирани. Ќе ги најдете во јазичето „Игнорирани“.",
            "Имиња на порталите",
        },
        ["Serbian"] = new[]
        {
            "Портали", "Одавде: $1", "Претрага...", "Сви", "Омиљени", "Сортирање: назив", "Мапа", "Листа", "Затвори", "Познатих портала: $1",
            "Скривено у близини: $1", "Откријте још истраживањем или за картографским столом", "Путуј", "Омиљени", "Картице", "Избор", "Следећи портал", "Откажи", "Путуј до...", "Портал без назива",
            "Још не знате ниједан други портал. Посетите портале или прочитајте картографски сто.", "Сви портали које знате су у близини (скривени).", "Још нема омиљених портала.", "Ниједан портал не одговара претрази.", "Нови портал: $1", "Портал $1 је нестао. Уклоњен са листе.", "Овде више нема портала. Уклоњен са листе.", "Портала научено са стола: $1", "Нових портала записано на сто: $1", "Записано портала: $1",
            "Изаберите портал на мапи", "Клик", "Десни клик", "Сортирање: најближи", "Сортирање: најдаљи", "Игнорисани", "Игнориши", "Нема игнорисаних портала.", "$1 је игнорисан. Налази се на картици „Игнорисани“.", "Преименуј пар",
            "Други портал је ван домета: преименован је само овај.",
            "Преименуј", "Портал је ван домашаја: посетите га да бисте га преименовали.", "Само портали", "Средњи клик", "Портали без назива се игноришу. Налазе се на картици „Игнорисани“.",
            "Имена портала",
        },
        ["Croatian"] = new[]
        {
            "Portali", "Odavde: $1", "Pretraži...", "Svi", "Omiljeni", "Poredak: naziv", "Karta", "Popis", "Zatvori", "Poznatih portala: $1",
            "Skriveno u blizini: $1", "Otkrijte više istraživanjem ili za kartografskim stolom", "Putuj", "Omiljeni", "Kartice", "Odabir", "Sljedeći portal", "Odustani", "Putuj do...", "Portal bez naziva",
            "Još ne znate nijedan drugi portal. Posjetite portale ili pročitajte kartografski stol.", "Svi portali koje znate su u blizini (skriveni).", "Još nema omiljenih portala.", "Nijedan portal ne odgovara pretrazi.", "Novi portal: $1", "Portal $1 je nestao. Uklonjen s popisa.", "Ovdje više nema portala. Uklonjen s popisa.", "Portala naučeno sa stola: $1", "Novih portala zapisano na stol: $1", "Zapisano portala: $1",
            "Odaberite portal na karti", "Klik", "Desni klik", "Poredak: najbliži", "Poredak: najdalji", "Zanemareni", "Zanemari", "Nema zanemarenih portala.", "$1 je zanemaren. Nalazi se na kartici „Zanemareni”.", "Preimenuj par",
            "Drugi portal je izvan dosega: preimenovan je samo ovaj.",
            "Preimenuj", "Portal je izvan dosega: posjetite ga da biste ga preimenovali.", "Samo portali", "Srednji klik", "Portali bez imena se zanemaruju. Nalaze se na kartici „Zanemareni”.",
            "Imena portala",
        },
        ["Polish"] = new[]
        {
            "Portale", "Stąd: $1", "Szukaj...", "Wszystkie", "Ulubione", "Sortuj: nazwa", "Mapa", "Lista", "Zamknij", "Znane portale: $1",
            "Ukryte w pobliżu: $1", "Poznawaj kolejne, eksplorując świat lub przy stole kartograficznym", "Podróżuj", "Ulubiony", "Karty", "Wybór", "Następny portal", "Anuluj", "Podróżuj do...", "Portal bez nazwy",
            "Nie znasz jeszcze innych portali. Odwiedzaj portale lub odczytaj stół kartograficzny.", "Wszystkie znane ci portale są w pobliżu (ukryte).", "Nie masz jeszcze ulubionych portali.", "Żaden portal nie pasuje do wyszukiwania.", "Nowy portal: $1", "Portal $1 zniknął. Usunięto z listy.", "Nie ma tu już portalu. Usunięto z listy.", "Portale poznane ze stołu: $1", "Nowe portale zapisane na stole: $1", "Zapisane portale: $1",
            "Wybierz portal na mapie", "Kliknięcie", "Prawy klik", "Sortuj: najbliższe", "Sortuj: najdalsze", "Ignorowane", "Ignoruj", "Brak ignorowanych portali.", "$1 jest ignorowany. Znajdziesz go w zakładce Ignorowane.", "Zmień nazwę obu",
            "Drugi portal jest poza zasięgiem: zmieniono nazwę tylko tego.",
            "Zmień nazwę", "Portal jest poza zasięgiem: odwiedź go, aby zmienić jego nazwę.", "Tylko portale", "Środkowy klik", "Portale bez nazwy są ignorowane. Znajdziesz je na karcie „Ignorowane”.",
            "Nazwy portali",
        },
        ["Czech"] = new[]
        {
            "Portály", "Odsud: $1", "Hledat...", "Vše", "Oblíbené", "Řadit: název", "Mapa", "Seznam", "Zavřít", "Známé portály: $1",
            "Skryté poblíž: $1", "Další poznáte průzkumem nebo u kartografického stolu", "Cestovat", "Oblíbený", "Karty", "Výběr", "Další portál", "Zrušit", "Cestovat do...", "Portál bez názvu",
            "Zatím neznáte žádný jiný portál. Navštivte portály nebo si přečtěte kartografický stůl.", "Všechny portály, které znáte, jsou poblíž (skryté).", "Zatím nemáte oblíbené portály.", "Hledání neodpovídá žádný portál.", "Nový portál: $1", "Portál $1 zmizel. Odebrán ze seznamu.", "Tady už žádný portál není. Odebrán ze seznamu.", "Portály poznané ze stolu: $1", "Nové portály zapsané na stůl: $1", "Zapsané portály: $1",
            "Vyberte portál na mapě", "Kliknutí", "Pravé kliknutí", "Řadit: nejbližší", "Řadit: nejvzdálenější", "Ignorované", "Ignorovat", "Žádné ignorované portály.", "$1 je ignorován. Najdete ho na kartě Ignorované.", "Přejmenovat oba",
            "Druhý portál je mimo dosah: přejmenován byl jen tento.",
            "Přejmenovat", "Portál je mimo dosah: navštivte ho, abyste ho mohli přejmenovat.", "Jen portály", "Prostřední klik", "Portály bez názvu jsou ignorovány. Najdete je na kartě „Ignorované“.",
            "Názvy portálů",
        },
        ["Slovak"] = new[]
        {
            "Portály", "Odtiaľto: $1", "Hľadať...", "Všetky", "Obľúbené", "Zoradiť: názov", "Mapa", "Zoznam", "Zavrieť", "Známe portály: $1",
            "Skryté nablízku: $1", "Ďalšie spoznáte prieskumom alebo pri kartografickom stole", "Cestovať", "Obľúbený", "Karty", "Výber", "Ďalší portál", "Zrušiť", "Cestovať do...", "Portál bez názvu",
            "Zatiaľ nepoznáte žiadny iný portál. Navštívte portály alebo si prečítajte kartografický stôl.", "Všetky portály, ktoré poznáte, sú nablízku (skryté).", "Zatiaľ nemáte obľúbené portály.", "Vyhľadávaniu nezodpovedá žiadny portál.", "Nový portál: $1", "Portál $1 zmizol. Odstránený zo zoznamu.", "Už tu nie je žiadny portál. Odstránený zo zoznamu.", "Portály spoznané zo stola: $1", "Nové portály zapísané na stôl: $1", "Zapísané portály: $1",
            "Vyberte portál na mape", "Kliknutie", "Pravé kliknutie", "Zoradiť: najbližšie", "Zoradiť: najvzdialenejšie", "Ignorované", "Ignorovať", "Žiadne ignorované portály.", "$1 je ignorovaný. Nájdete ho na karte Ignorované.", "Premenovať oba",
            "Druhý portál je mimo dosahu: premenovaný bol len tento.",
            "Premenovať", "Portál je mimo dosahu: navštívte ho, aby ste ho mohli premenovať.", "Len portály", "Stredný klik", "Portály bez názvu sú ignorované. Nájdete ich na karte „Ignorované“.",
            "Názvy portálov",
        },
        ["German"] = new[]
        {
            "Portale", "Von: $1", "Suchen...", "Alle", "Favoriten", "Sortieren: Name", "Karte", "Liste", "Schließen", "Bekannte Portale: $1",
            "In der Nähe ausgeblendet: $1", "Entdecke weitere beim Erkunden oder am Kartentisch", "Reisen", "Favorit", "Reiter", "Auswählen", "Nächstes Portal", "Abbrechen", "Reisen nach...", "Unbenanntes Portal",
            "Du kennst noch kein anderes Portal. Besuche Portale oder lies einen Kartentisch.", "Alle Portale, die du kennst, sind in der Nähe (ausgeblendet).", "Noch keine Lieblingsportale.", "Kein Portal passt zur Suche.", "Neues Portal: $1", "Portal $1 ist verschwunden. Aus deiner Liste entfernt.", "Hier gibt es kein Portal mehr. Aus deiner Liste entfernt.", "Vom Tisch gelernte Portale: $1", "Neu auf dem Tisch eingetragene Portale: $1", "Eingetragene Portale: $1",
            "Wähle ein Portal auf der Karte", "Klick", "Rechtsklick", "Sortieren: nächste", "Sortieren: fernste", "Ignoriert", "Ignorieren", "Keine ignorierten Portale.", "$1 wird ignoriert. Es steht im Tab „Ignoriert“.", "Beide Enden umbenennen",
            "Das andere Portal ist außer Reichweite: nur dieses wurde umbenannt.",
            "Umbenennen", "Dieses Portal ist außer Reichweite: Besuche es, um es umzubenennen.", "Nur Portale", "Mittelklick", "Unbenannte Portale werden ignoriert. Sie sind im Tab „Ignoriert“.",
            "Portalnamen",
        },
        ["Dutch"] = new[]
        {
            "Portalen", "Vanaf: $1", "Zoeken...", "Alle", "Favorieten", "Sorteren: naam", "Kaart", "Lijst", "Sluiten", "Bekende portalen: $1",
            "Verborgen in de buurt: $1", "Ontdek er meer door te verkennen of bij een kaarttafel", "Reizen", "Favoriet", "Tabbladen", "Kiezen", "Volgend portaal", "Annuleren", "Reizen naar...", "Naamloos portaal",
            "Je kent nog geen ander portaal. Bezoek portalen of lees een kaarttafel.", "Alle portalen die je kent zijn dichtbij (verborgen).", "Nog geen favoriete portalen.", "Geen portaal komt overeen met de zoekopdracht.", "Nieuw portaal: $1", "Portaal $1 is verdwenen. Verwijderd uit je lijst.", "Hier is geen portaal meer. Verwijderd uit je lijst.", "Portalen geleerd van de tafel: $1", "Nieuwe portalen op de tafel vastgelegd: $1", "Vastgelegde portalen: $1",
            "Kies een portaal op de kaart", "Klik", "Rechtsklik", "Sorteren: dichtstbij", "Sorteren: verst weg", "Genegeerd", "Negeren", "Geen genegeerde portalen.", "$1 wordt genegeerd. Je vindt het op het tabblad Genegeerd.", "Beide uiteinden hernoemen",
            "Het andere portaal is buiten bereik: alleen dit is hernoemd.",
            "Hernoemen", "Dit portaal is buiten bereik: bezoek het om het te hernoemen.", "Alleen portalen", "Middelklik", "Naamloze portalen worden genegeerd. Je vindt ze op het tabblad Genegeerd.",
            "Portaalnamen",
        },
        ["Swedish"] = new[]
        {
            "Portaler", "Från: $1", "Sök...", "Alla", "Favoriter", "Sortera: namn", "Karta", "Lista", "Stäng", "Kända portaler: $1",
            "Dolda i närheten: $1", "Lär dig fler genom att utforska eller vid ett kartbord", "Res", "Favorit", "Flikar", "Välj", "Nästa portal", "Avbryt", "Res till...", "Namnlös portal",
            "Du känner inte till någon annan portal än. Besök portaler eller läs ett kartbord.", "Alla portaler du känner till är i närheten (dolda).", "Inga favoritportaler än.", "Ingen portal matchar sökningen.", "Ny portal: $1", "Portalen $1 är borta. Borttagen från listan.", "Här finns ingen portal längre. Borttagen från listan.", "Portaler lärda från bordet: $1", "Nya portaler sparade på bordet: $1", "Sparade portaler: $1",
            "Välj en portal på kartan", "Klick", "Högerklick", "Sortera: närmast", "Sortera: längst bort", "Ignorerade", "Ignorera", "Inga ignorerade portaler.", "$1 ignoreras. Den finns under fliken Ignorerade.", "Byt namn på båda",
            "Den andra portalen är utom räckhåll: bara den här fick nytt namn.",
            "Byt namn", "Portalen är utom räckhåll: besök den för att byta namn på den.", "Bara portaler", "Mittenklick", "Namnlösa portaler ignoreras. De finns under fliken Ignorerade.",
            "Portalnamn",
        },
        ["Danish"] = new[]
        {
            "Portaler", "Herfra: $1", "Søg...", "Alle", "Favoritter", "Sortér: navn", "Kort", "Liste", "Luk", "Kendte portaler: $1",
            "Skjulte i nærheden: $1", "Lær flere at kende ved at udforske eller ved et kortbord", "Rejs", "Favorit", "Faner", "Vælg", "Næste portal", "Annullér", "Rejs til...", "Navnløs portal",
            "Du kender endnu ingen anden portal. Besøg portaler eller læs et kortbord.", "Alle de portaler, du kender, er i nærheden (skjult).", "Ingen favoritportaler endnu.", "Ingen portal matcher søgningen.", "Ny portal: $1", "Portalen $1 er væk. Fjernet fra din liste.", "Her er ikke længere nogen portal. Fjernet fra din liste.", "Portaler lært fra bordet: $1", "Nye portaler gemt på bordet: $1", "Gemte portaler: $1",
            "Vælg en portal på kortet", "Klik", "Højreklik", "Sortér: nærmeste", "Sortér: fjerneste", "Ignorerede", "Ignorér", "Ingen ignorerede portaler.", "$1 ignoreres. Den ligger under fanen Ignorerede.", "Omdøb begge ender",
            "Den anden portal er uden for rækkevidde: kun denne blev omdøbt.",
            "Omdøb", "Portalen er uden for rækkevidde: besøg den for at omdøbe den.", "Kun portaler", "Midterklik", "Navnløse portaler ignoreres. De ligger under fanen Ignorerede.",
            "Portalnavne",
        },
        ["Norwegian"] = new[]
        {
            "Portaler", "Herfra: $1", "Søk...", "Alle", "Favoritter", "Sorter: navn", "Kart", "Liste", "Lukk", "Kjente portaler: $1",
            "Skjult i nærheten: $1", "Lær flere ved å utforske eller ved et kartbord", "Reis", "Favoritt", "Faner", "Velg", "Neste portal", "Avbryt", "Reis til...", "Navnløs portal",
            "Du kjenner ingen andre portaler ennå. Besøk portaler eller les et kartbord.", "Alle portalene du kjenner er i nærheten (skjult).", "Ingen favorittportaler ennå.", "Ingen portal passer til søket.", "Ny portal: $1", "Portalen $1 er borte. Fjernet fra listen.", "Her er det ikke lenger noen portal. Fjernet fra listen.", "Portaler lært fra bordet: $1", "Nye portaler lagret på bordet: $1", "Lagrede portaler: $1",
            "Velg en portal på kartet", "Klikk", "Høyreklikk", "Sorter: nærmeste", "Sorter: fjerneste", "Ignorerte", "Ignorer", "Ingen ignorerte portaler.", "$1 ignoreres. Den ligger under fanen Ignorerte.", "Gi begge nytt navn",
            "Den andre portalen er utenfor rekkevidde: bare denne fikk nytt navn.",
            "Gi nytt navn", "Portalen er utenfor rekkevidde: besøk den for å gi den nytt navn.", "Bare portaler", "Midtklikk", "Navnløse portaler ignoreres. De ligger under fanen Ignorerte.",
            "Portalnavn",
        },
        ["Icelandic"] = new[]
        {
            "Gáttir", "Héðan: $1", "Leita...", "Allar", "Eftirlæti", "Raða: nafn", "Kort", "Listi", "Loka", "Þekktar gáttir: $1",
            "Faldar í nágrenninu: $1", "Lærðu fleiri með því að kanna heiminn eða við kortaborð", "Ferðast", "Eftirlæti", "Flipar", "Velja", "Næsta gátt", "Hætta við", "Ferðast til...", "Nafnlaus gátt",
            "Þú þekkir enga aðra gátt enn. Heimsæktu gáttir eða lestu kortaborð.", "Allar gáttirnar sem þú þekkir eru nálægt (faldar).", "Engar eftirlætisgáttir enn.", "Engin gátt passar við leitina.", "Ný gátt: $1", "Gáttin $1 er horfin. Fjarlægð af listanum.", "Hér er ekki lengur gátt. Fjarlægð af listanum.", "Gáttir lærðar af borðinu: $1", "Nýjar gáttir skráðar á borðið: $1", "Skráðar gáttir: $1",
            "Veldu gátt á kortinu", "Smella", "Hægrismella", "Raða: næst", "Raða: fjærst", "Hunsaðar", "Hunsa", "Engar hunsaðar gáttir.", "$1 er hunsuð. Hún er á flipanum Hunsaðar.", "Endurnefna báða enda",
            "Hin gáttin er utan seilingar: aðeins þessi var endurnefnd.",
            "Endurnefna", "Gáttin er utan seilingar: heimsæktu hana til að endurnefna hana.", "Aðeins gáttir", "Miðjusmella", "Nafnlausar gáttir eru hunsaðar. Þær eru á flipanum Hunsaðar.",
            "Heiti gátta",
        },
        ["Finnish"] = new[]
        {
            "Portaalit", "Täältä: $1", "Hae...", "Kaikki", "Suosikit", "Järjestys: nimi", "Kartta", "Lista", "Sulje", "Tunnetut portaalit: $1",
            "Piilotettu lähellä: $1", "Opi lisää tutkimalla tai karttapöydän ääressä", "Matkusta", "Suosikki", "Välilehdet", "Valitse", "Seuraava portaali", "Peruuta", "Matkusta kohteeseen...", "Nimetön portaali",
            "Et tunne vielä muita portaaleja. Käy portaaleilla tai lue karttapöytä.", "Kaikki tuntemasi portaalit ovat lähellä (piilotettu).", "Ei vielä suosikkiportaaleja.", "Mikään portaali ei vastaa hakua.", "Uusi portaali: $1", "Portaali $1 on poissa. Poistettu listalta.", "Täällä ei ole enää portaalia. Poistettu listalta.", "Pöydältä opitut portaalit: $1", "Pöydälle tallennetut uudet portaalit: $1", "Tallennetut portaalit: $1",
            "Valitse portaali kartalta", "Napsautus", "Oikea napsautus", "Järjestys: lähimmät", "Järjestys: kaukaisimmat", "Ohitetut", "Ohita", "Ei ohitettuja portaaleja.", "$1 ohitetaan. Löydät sen Ohitetut-välilehdeltä.", "Nimeä molemmat päät",
            "Toinen portaali on ulottumattomissa: vain tämä nimettiin uudelleen.",
            "Nimeä uudelleen", "Portaali on ulottumattomissa: käy sen luona nimetäksesi sen uudelleen.", "Vain portaalit", "Keskinapsautus", "Nimettömät portaalit ohitetaan. Löydät ne Ohitetut-välilehdeltä.",
            "Portaalien nimet",
        },
        ["French"] = new[]
        {
            "Portails", "Depuis : $1", "Rechercher...", "Tous", "Favoris", "Tri : nom", "Carte", "Liste", "Fermer", "Portails connus : $1",
            "Masqués à proximité : $1", "Découvrez-en d'autres en explorant ou à une table de cartographie", "Voyager", "Favori", "Onglets", "Choisir", "Portail suivant", "Annuler", "Voyager vers...", "Portail sans nom",
            "Vous ne connaissez encore aucun autre portail. Visitez des portails ou consultez une table de cartographie.", "Tous les portails que vous connaissez sont à proximité (masqués).", "Aucun portail favori pour l'instant.", "Aucun portail ne correspond à la recherche.", "Nouveau portail : $1", "Le portail $1 a disparu. Retiré de votre liste.", "Il n'y a plus de portail ici. Retiré de votre liste.", "Portails appris sur la table : $1", "Nouveaux portails inscrits sur la table : $1", "Portails inscrits : $1",
            "Choisissez un portail sur la carte", "Clic", "Clic droit", "Tri : plus proches", "Tri : plus lointains", "Ignorés", "Ignorer", "Aucun portail ignoré.", "$1 est ignoré. Il se trouve dans l'onglet Ignorés.", "Renommer les deux",
            "L'autre portail est hors de portée : seul celui-ci a été renommé.",
            "Renommer", "Ce portail est hors de portée : rendez-vous-y pour le renommer.", "Portails seuls", "Clic molette", "Les portails sans nom sont ignorés. Ils se trouvent dans l'onglet Ignorés.",
            "Noms des portails",
        },
        ["Italian"] = new[]
        {
            "Portali", "Da: $1", "Cerca...", "Tutti", "Preferiti", "Ordina: nome", "Mappa", "Elenco", "Chiudi", "Portali conosciuti: $1",
            "Nascosti nelle vicinanze: $1", "Scoprine altri esplorando o a un tavolo cartografico", "Viaggia", "Preferito", "Schede", "Seleziona", "Portale successivo", "Annulla", "Viaggia verso...", "Portale senza nome",
            "Non conosci ancora nessun altro portale. Visita i portali o leggi un tavolo cartografico.", "Tutti i portali che conosci sono vicini (nascosti).", "Ancora nessun portale preferito.", "Nessun portale corrisponde alla ricerca.", "Nuovo portale: $1", "Il portale $1 è scomparso. Rimosso dall'elenco.", "Qui non c'è più nessun portale. Rimosso dall'elenco.", "Portali appresi dal tavolo: $1", "Nuovi portali registrati sul tavolo: $1", "Portali registrati: $1",
            "Scegli un portale sulla mappa", "Clic", "Clic destro", "Ordina: più vicini", "Ordina: più lontani", "Ignorati", "Ignora", "Nessun portale ignorato.", "$1 è ignorato. Lo trovi nella scheda Ignorati.", "Rinomina entrambi",
            "L'altro portale è fuori portata: è stato rinominato solo questo.",
            "Rinomina", "Questo portale è fuori portata: visitalo per rinominarlo.", "Solo portali", "Clic centrale", "I portali senza nome sono ignorati. Si trovano nella scheda Ignorati.",
            "Nomi dei portali",
        },
        ["Spanish"] = new[]
        {
            "Portales", "Desde: $1", "Buscar...", "Todos", "Favoritos", "Ordenar: nombre", "Mapa", "Lista", "Cerrar", "Portales conocidos: $1",
            "Ocultos cerca: $1", "Descubre más explorando o en una mesa de cartografía", "Viajar", "Favorito", "Pestañas", "Elegir", "Siguiente portal", "Cancelar", "Viajar a...", "Portal sin nombre",
            "Aún no conoces ningún otro portal. Visita portales o lee una mesa de cartografía.", "Todos los portales que conoces están cerca (ocultos).", "Aún no hay portales favoritos.", "Ningún portal coincide con la búsqueda.", "Nuevo portal: $1", "El portal $1 ha desaparecido. Eliminado de tu lista.", "Aquí ya no hay ningún portal. Eliminado de tu lista.", "Portales aprendidos de la mesa: $1", "Nuevos portales registrados en la mesa: $1", "Portales registrados: $1",
            "Elige un portal en el mapa", "Clic", "Clic derecho", "Ordenar: más cercanos", "Ordenar: más lejanos", "Ignorados", "Ignorar", "No hay portales ignorados.", "$1 está ignorado. Está en la pestaña Ignorados.", "Renombrar ambos",
            "El otro portal está fuera de alcance: solo se renombró este.",
            "Renombrar", "Este portal está fuera de alcance: visítalo para renombrarlo.", "Solo portales", "Clic central", "Los portales sin nombre se ignoran. Están en la pestaña Ignorados.",
            "Nombres de portales",
        },
        ["Portuguese_European"] = new[]
        {
            "Portais", "De: $1", "Pesquisar...", "Todos", "Favoritos", "Ordenar: nome", "Mapa", "Lista", "Fechar", "Portais conhecidos: $1",
            "Ocultos por perto: $1", "Descobre mais ao explorar ou numa mesa de cartografia", "Viajar", "Favorito", "Separadores", "Escolher", "Portal seguinte", "Cancelar", "Viajar para...", "Portal sem nome",
            "Ainda não conheces nenhum outro portal. Visita portais ou lê uma mesa de cartografia.", "Todos os portais que conheces estão por perto (ocultos).", "Ainda não há portais favoritos.", "Nenhum portal corresponde à pesquisa.", "Novo portal: $1", "O portal $1 desapareceu. Removido da tua lista.", "Já não há nenhum portal aqui. Removido da tua lista.", "Portais aprendidos na mesa: $1", "Novos portais registados na mesa: $1", "Portais registados: $1",
            "Escolhe um portal no mapa", "Clique", "Clique direito", "Ordenar: mais próximos", "Ordenar: mais distantes", "Ignorados", "Ignorar", "Não há portais ignorados.", "$1 está ignorado. Está no separador Ignorados.", "Renomear ambos",
            "O outro portal está fora de alcance: só este foi renomeado.",
            "Mudar o nome", "Este portal está fora de alcance: visite-o para lhe mudar o nome.", "Só portais", "Clique do meio", "Os portais sem nome são ignorados. Estão no separador Ignorados.",
            "Nomes dos portais",
        },
        ["Portuguese_Brazilian"] = new[]
        {
            "Portais", "De: $1", "Buscar...", "Todos", "Favoritos", "Ordenar: nome", "Mapa", "Lista", "Fechar", "Portais conhecidos: $1",
            "Ocultos por perto: $1", "Descubra mais explorando ou em uma mesa de cartografia", "Viajar", "Favorito", "Abas", "Escolher", "Próximo portal", "Cancelar", "Viajar para...", "Portal sem nome",
            "Você ainda não conhece nenhum outro portal. Visite portais ou leia uma mesa de cartografia.", "Todos os portais que você conhece estão por perto (ocultos).", "Ainda não há portais favoritos.", "Nenhum portal corresponde à busca.", "Novo portal: $1", "O portal $1 sumiu. Removido da sua lista.", "Não há mais nenhum portal aqui. Removido da sua lista.", "Portais aprendidos na mesa: $1", "Novos portais registrados na mesa: $1", "Portais registrados: $1",
            "Escolha um portal no mapa", "Clique", "Clique direito", "Ordenar: mais próximos", "Ordenar: mais distantes", "Ignorados", "Ignorar", "Não há portais ignorados.", "$1 está ignorado. Está na aba Ignorados.", "Renomear ambos",
            "O outro portal está fora de alcance: só este foi renomeado.",
            "Renomear", "Este portal está fora de alcance: visite-o para renomeá-lo.", "Só portais", "Clique do meio", "Os portais sem nome são ignorados. Eles estão na aba Ignorados.",
            "Nomes dos portais",
        },
        ["Romanian"] = new[]
        {
            "Portaluri", "De aici: $1", "Caută...", "Toate", "Favorite", "Sortare: nume", "Hartă", "Listă", "Închide", "Portaluri cunoscute: $1",
            "Ascunse în apropiere: $1", "Descoperă altele explorând sau la o masă de cartografie", "Călătorește", "Favorit", "File", "Alege", "Portalul următor", "Anulează", "Călătorește spre...", "Portal fără nume",
            "Nu cunoști încă niciun alt portal. Vizitează portaluri sau citește o masă de cartografie.", "Toate portalurile pe care le cunoști sunt în apropiere (ascunse).", "Încă nu ai portaluri favorite.", "Niciun portal nu se potrivește căutării.", "Portal nou: $1", "Portalul $1 a dispărut. Eliminat din listă.", "Aici nu mai este niciun portal. Eliminat din listă.", "Portaluri învățate de la masă: $1", "Portaluri noi înregistrate pe masă: $1", "Portaluri înregistrate: $1",
            "Alege un portal pe hartă", "Clic", "Clic dreapta", "Sortare: cele mai apropiate", "Sortare: cele mai îndepărtate", "Ignorate", "Ignoră", "Niciun portal ignorat.", "$1 este ignorat. Îl găsești în fila Ignorate.", "Redenumește ambele",
            "Celălalt portal nu poate fi atins: doar acesta a fost redenumit.",
            "Redenumește", "Acest portal este în afara razei: vizitează-l pentru a-l redenumi.", "Doar portaluri", "Clic din mijloc", "Portalurile fără nume sunt ignorate. Le găsești în fila Ignorate.",
            "Numele portalurilor",
        },
        ["Hungarian"] = new[]
        {
            "Portálok", "Innen: $1", "Keresés...", "Összes", "Kedvencek", "Rendezés: név", "Térkép", "Lista", "Bezárás", "Ismert portálok: $1",
            "Rejtett a közelben: $1", "Fedezz fel többet felfedezéssel vagy egy térképasztalnál", "Utazás", "Kedvenc", "Fülek", "Kiválasztás", "Következő portál", "Mégse", "Utazás ide...", "Névtelen portál",
            "Még nem ismersz más portált. Látogass el portálokhoz, vagy olvass el egy térképasztalt.", "Az összes ismert portálod a közelben van (rejtve).", "Még nincsenek kedvenc portáljaid.", "Egy portál sem felel meg a keresésnek.", "Új portál: $1", "A(z) $1 portál eltűnt. Eltávolítva a listáról.", "Itt már nincs portál. Eltávolítva a listáról.", "Az asztalról megismert portálok: $1", "Az asztalra felírt új portálok: $1", "Felírt portálok: $1",
            "Válassz egy portált a térképen", "Kattintás", "Jobb kattintás", "Rendezés: legközelebbi", "Rendezés: legtávolabbi", "Mellőzöttek", "Mellőzés", "Nincsenek mellőzött portálok.", "$1 mellőzve. A Mellőzöttek lapon találod.", "Mindkét vég átnevezése",
            "A másik portál nem érhető el: csak ez lett átnevezve.",
            "Átnevezés", "Ez a portál elérhetetlen: látogasd meg, hogy átnevezhesd.", "Csak portálok", "Középső kattintás", "A névtelen portálok mellőzve vannak. A Mellőzöttek lapon találod őket.",
            "Portálnevek",
        },
        ["Lithuanian"] = new[]
        {
            "Portalai", "Iš čia: $1", "Ieškoti...", "Visi", "Mėgstami", "Rikiuoti: pavadinimas", "Žemėlapis", "Sąrašas", "Uždaryti", "Žinomi portalai: $1",
            "Paslėpti netoliese: $1", "Sužinokite daugiau tyrinėdami arba prie kartografijos stalo", "Keliauti", "Mėgstamas", "Skirtukai", "Pasirinkti", "Kitas portalas", "Atšaukti", "Keliauti į...", "Portalas be pavadinimo",
            "Dar nežinote jokio kito portalo. Aplankykite portalus arba perskaitykite kartografijos stalą.", "Visi jums žinomi portalai yra netoliese (paslėpti).", "Dar nėra mėgstamų portalų.", "Paieškos neatitinka joks portalas.", "Naujas portalas: $1", "Portalas $1 dingo. Pašalintas iš sąrašo.", "Čia nebėra portalo. Pašalintas iš sąrašo.", "Portalai, sužinoti iš stalo: $1", "Nauji portalai, įrašyti ant stalo: $1", "Įrašyti portalai: $1",
            "Pasirinkite portalą žemėlapyje", "Spustelėjimas", "Dešinysis spustelėjimas", "Rikiuoti: artimiausi", "Rikiuoti: tolimiausi", "Ignoruojami", "Ignoruoti", "Ignoruojamų portalų nėra.", "$1 ignoruojamas. Jį rasite skirtuke „Ignoruojami“.", "Pervadinti abu galus",
            "Kitas portalas nepasiekiamas: pervadintas tik šis.",
            "Pervadinti", "Šis portalas nepasiekiamas: aplankykite jį, kad galėtumėte pervadinti.", "Tik portalai", "Vidurinis spustelėjimas", "Portalai be pavadinimo ignoruojami. Juos rasite skirtuke „Ignoruojami“.",
            "Portalų pavadinimai",
        },
        ["Latvian"] = new[]
        {
            "Portāli", "No šejienes: $1", "Meklēt...", "Visi", "Izlase", "Kārtot: nosaukums", "Karte", "Saraksts", "Aizvērt", "Zināmie portāli: $1",
            "Paslēpti tuvumā: $1", "Uzziniet vairāk, pētot pasauli vai pie kartogrāfijas galda", "Ceļot", "Izlasē", "Cilnes", "Izvēlēties", "Nākamais portāls", "Atcelt", "Ceļot uz...", "Portāls bez nosaukuma",
            "Jūs vēl nezināt nevienu citu portālu. Apmeklējiet portālus vai izlasiet kartogrāfijas galdu.", "Visi jums zināmie portāli ir tuvumā (paslēpti).", "Izlasē vēl nav portālu.", "Meklēšanai neatbilst neviens portāls.", "Jauns portāls: $1", "Portāls $1 ir pazudis. Noņemts no saraksta.", "Šeit vairs nav portāla. Noņemts no saraksta.", "No galda uzzināti portāli: $1", "Uz galda ierakstīti jauni portāli: $1", "Ierakstīti portāli: $1",
            "Izvēlieties portālu kartē", "Klikšķis", "Labais klikšķis", "Kārtot: tuvākie", "Kārtot: tālākie", "Ignorētie", "Ignorēt", "Nav ignorētu portālu.", "$1 tiek ignorēts. Tas ir cilnē Ignorētie.", "Pārdēvēt abus galus",
            "Otrs portāls nav sasniedzams: pārdēvēts tikai šis.",
            "Pārdēvēt", "Šis portāls nav sasniedzams: apmeklējiet to, lai to pārdēvētu.", "Tikai portāli", "Vidējais klikšķis", "Portāli bez nosaukuma tiek ignorēti. Tie ir cilnē “Ignorētie”.",
            "Portālu nosaukumi",
        },
        ["Turkish"] = new[]
        {
            "Portallar", "Buradan: $1", "Ara...", "Tümü", "Favoriler", "Sırala: ad", "Harita", "Liste", "Kapat", "Bilinen portallar: $1",
            "Yakında gizlenen: $1", "Keşfederek veya bir harita masasında daha fazlasını öğren", "Yolculuk et", "Favori", "Sekmeler", "Seç", "Sonraki portal", "İptal", "Şuraya yolculuk et...", "İsimsiz portal",
            "Henüz başka bir portal bilmiyorsun. Portalları ziyaret et veya bir harita masasını oku.", "Bildiğin tüm portallar yakında (gizli).", "Henüz favori portal yok.", "Aramayla eşleşen portal yok.", "Yeni portal: $1", "$1 portalı yok olmuş. Listenden kaldırıldı.", "Burada artık portal yok. Listenden kaldırıldı.", "Masadan öğrenilen portallar: $1", "Masaya kaydedilen yeni portallar: $1", "Kaydedilen portallar: $1",
            "Haritada bir portal seç", "Tıkla", "Sağ tıkla", "Sırala: en yakın", "Sırala: en uzak", "Yok sayılanlar", "Yok say", "Yok sayılan portal yok.", "$1 yok sayılıyor. Yok sayılanlar sekmesinde.", "İki ucu da yeniden adlandır",
            "Diğer portala ulaşılamıyor: yalnızca bu yeniden adlandırıldı.",
            "Yeniden adlandır", "Bu geçit erişim dışında: yeniden adlandırmak için onu ziyaret edin.", "Yalnızca geçitler", "Orta tık", "Adsız geçitler yok sayılıyor. Yok sayılanlar sekmesindeler.",
            "Geçit adları",
        },
        ["Greek"] = new[]
        {
            "Πύλες", "Από: $1", "Αναζήτηση...", "Όλες", "Αγαπημένες", "Ταξινόμηση: όνομα", "Χάρτης", "Λίστα", "Κλείσιμο", "Γνωστές πύλες: $1",
            "Κρυμμένες κοντά: $1", "Μάθετε περισσότερες εξερευνώντας ή σε ένα τραπέζι χαρτογραφίας", "Ταξίδι", "Αγαπημένη", "Καρτέλες", "Επιλογή", "Επόμενη πύλη", "Ακύρωση", "Ταξίδι προς...", "Πύλη χωρίς όνομα",
            "Δεν γνωρίζετε ακόμη καμία άλλη πύλη. Επισκεφθείτε πύλες ή διαβάστε ένα τραπέζι χαρτογραφίας.", "Όλες οι πύλες που γνωρίζετε είναι κοντά (κρυμμένες).", "Δεν υπάρχουν ακόμη αγαπημένες πύλες.", "Καμία πύλη δεν ταιριάζει με την αναζήτηση.", "Νέα πύλη: $1", "Η πύλη $1 χάθηκε. Αφαιρέθηκε από τη λίστα σας.", "Δεν υπάρχει πια πύλη εδώ. Αφαιρέθηκε από τη λίστα σας.", "Πύλες από το τραπέζι: $1", "Νέες πύλες καταγράφηκαν στο τραπέζι: $1", "Καταγεγραμμένες πύλες: $1",
            "Επιλέξτε μια πύλη στον χάρτη", "Κλικ", "Δεξί κλικ", "Ταξινόμηση: κοντινότερες", "Ταξινόμηση: μακρινότερες", "Αγνοημένες", "Αγνόηση", "Δεν υπάρχουν αγνοημένες πύλες.", "Η $1 αγνοείται. Θα τη βρείτε στην καρτέλα Αγνοημένες.", "Μετονομασία και των δύο άκρων",
            "Η άλλη πύλη είναι εκτός εμβέλειας: μετονομάστηκε μόνο αυτή.",
            "Μετονομασία", "Αυτή η πύλη είναι εκτός εμβέλειας: επισκεφθείτε την για να τη μετονομάσετε.", "Μόνο πύλες", "Μεσαίο κλικ", "Οι πύλες χωρίς όνομα αγνοούνται. Βρίσκονται στην καρτέλα Αγνοημένες.",
            "Ονόματα πυλών",
        },
        ["Georgian"] = new[]
        {
            "პორტალები", "აქედან: $1", "ძიება...", "ყველა", "რჩეულები", "დალაგება: სახელი", "რუკა", "სია", "დახურვა", "ცნობილი პორტალები: $1",
            "ახლოს დამალული: $1", "გაიგეთ მეტი მსოფლიოს კვლევით ან კარტოგრაფიულ მაგიდასთან", "მოგზაურობა", "რჩეული", "ჩანართები", "არჩევა", "შემდეგი პორტალი", "გაუქმება", "მოგზაურობა...", "უსახელო პორტალი",
            "სხვა პორტალი ჯერ არ იცით. ეწვიეთ პორტალებს ან წაიკითხეთ კარტოგრაფიული მაგიდა.", "ყველა ცნობილი პორტალი ახლოსაა (დამალულია).", "რჩეული პორტალები ჯერ არ არის.", "ძიებას არცერთი პორტალი არ შეესაბამება.", "ახალი პორტალი: $1", "პორტალი $1 გაქრა. ამოშლილია სიიდან.", "აქ პორტალი აღარ არის. ამოშლილია სიიდან.", "მაგიდიდან ნასწავლი პორტალები: $1", "მაგიდაზე ჩაწერილი ახალი პორტალები: $1", "ჩაწერილი პორტალები: $1",
            "აირჩიეთ პორტალი რუკაზე", "დაწკაპუნება", "მარჯვენა დაწკაპუნება", "დალაგება: უახლოესი", "დალაგება: უშორესი", "იგნორირებული", "იგნორირება", "იგნორირებული პორტალები არ არის.", "$1 იგნორირებულია. ის იგნორირებულის ჩანართშია.", "ორივე ბოლოს გადარქმევა",
            "მეორე პორტალი მიუწვდომელია: სახელი მხოლოდ ამას შეეცვალა.",
            "გადარქმევა", "ეს პორტალი მიუწვდომელია: ეწვიეთ მას, რომ სახელი გადაარქვათ.", "მხოლოდ პორტალები", "შუა დაწკაპუნება", "უსახელო პორტალები იგნორირებულია. ისინი „იგნორირებულის“ ჩანართზეა.",
            "პორტალების სახელები",
        },
        ["Hindi"] = new[]
        {
            "पोर्टल", "यहाँ से: $1", "खोजें...", "सभी", "पसंदीदा", "क्रम: नाम", "नक्शा", "सूची", "बंद करें", "ज्ञात पोर्टल: $1",
            "पास में छिपे: $1", "खोज करके या मानचित्र मेज़ पर और पोर्टल जानें", "यात्रा करें", "पसंदीदा बनाएँ", "टैब", "चुनें", "अगला पोर्टल", "रद्द करें", "यहाँ जाएँ...", "बिना नाम का पोर्टल",
            "आप अभी कोई और पोर्टल नहीं जानते। पोर्टलों पर जाएँ या मानचित्र मेज़ पढ़ें।", "आपके जाने हुए सभी पोर्टल पास में हैं (छिपे हुए)।", "अभी कोई पसंदीदा पोर्टल नहीं है।", "खोज से कोई पोर्टल मेल नहीं खाता।", "नया पोर्टल: $1", "पोर्टल $1 गायब हो गया। आपकी सूची से हटाया गया।", "यहाँ अब कोई पोर्टल नहीं है। आपकी सूची से हटाया गया।", "मेज़ से जाने गए पोर्टल: $1", "मेज़ पर दर्ज नए पोर्टल: $1", "दर्ज पोर्टल: $1",
            "नक्शे पर एक पोर्टल चुनें", "क्लिक", "राइट-क्लिक", "क्रम: सबसे पास", "क्रम: सबसे दूर", "अनदेखे", "अनदेखा करें", "कोई अनदेखा पोर्टल नहीं है।", "$1 को अनदेखा किया गया है। यह अनदेखे टैब में है।", "दोनों सिरों का नाम बदलें",
            "दूसरा पोर्टल पहुँच से बाहर है: केवल इसी का नाम बदला गया।",
            "नाम बदलें", "यह पोर्टल पहुँच से बाहर है: इसका नाम बदलने के लिए वहाँ जाएँ।", "केवल पोर्टल", "मिडल-क्लिक", "बिना नाम वाले पोर्टल अनदेखे हैं। वे अनदेखे टैब में हैं।",
            "पोर्टल के नाम",
        },
        ["Thai"] = new[]
        {
            "พอร์ทัล", "จากที่นี่: $1", "ค้นหา...", "ทั้งหมด", "รายการโปรด", "เรียง: ชื่อ", "แผนที่", "รายการ", "ปิด", "พอร์ทัลที่รู้จัก: $1",
            "ซ่อนที่อยู่ใกล้: $1", "เรียนรู้เพิ่มเติมด้วยการสำรวจหรือที่โต๊ะแผนที่", "เดินทาง", "ชื่นชอบ", "แท็บ", "เลือก", "พอร์ทัลถัดไป", "ยกเลิก", "เดินทางไปยัง...", "พอร์ทัลไม่มีชื่อ",
            "คุณยังไม่รู้จักพอร์ทัลอื่น ไปเยือนพอร์ทัลหรืออ่านโต๊ะแผนที่", "พอร์ทัลทั้งหมดที่คุณรู้จักอยู่ใกล้ๆ (ซ่อนอยู่)", "ยังไม่มีพอร์ทัลโปรด", "ไม่มีพอร์ทัลที่ตรงกับการค้นหา", "พอร์ทัลใหม่: $1", "พอร์ทัล $1 หายไปแล้ว นำออกจากรายการแล้ว", "ที่นี่ไม่มีพอร์ทัลแล้ว นำออกจากรายการแล้ว", "พอร์ทัลที่เรียนรู้จากโต๊ะ: $1", "พอร์ทัลใหม่ที่บันทึกลงโต๊ะ: $1", "พอร์ทัลที่บันทึกไว้: $1",
            "เลือกพอร์ทัลบนแผนที่", "คลิก", "คลิกขวา", "เรียง: ใกล้สุด", "เรียง: ไกลสุด", "ที่ละเว้น", "ละเว้น", "ไม่มีพอร์ทัลที่ละเว้น", "$1 ถูกละเว้นแล้ว อยู่ในแท็บที่ละเว้น", "เปลี่ยนชื่อทั้งสองฝั่ง",
            "พอร์ทัลอีกฝั่งอยู่นอกระยะ: เปลี่ยนชื่อเฉพาะฝั่งนี้",
            "เปลี่ยนชื่อ", "พอร์ทัลนี้อยู่นอกระยะ: ไปที่พอร์ทัลเพื่อเปลี่ยนชื่อ", "เฉพาะพอร์ทัล", "คลิกกลาง", "พอร์ทัลที่ไม่มีชื่อถูกละเว้น อยู่ในแท็บที่ละเว้น",
            "ชื่อพอร์ทัล",
        },
        ["Chinese"] = new[]
        {
            "传送门", "出发地:$1", "搜索...", "全部", "收藏", "排序:名称", "地图", "列表", "关闭", "已知传送门:$1",
            "附近已隐藏:$1", "通过探索或在制图台了解更多", "传送", "收藏", "标签页", "选择", "下一个传送门", "取消", "传送到...", "未命名传送门",
            "你还不知道其他传送门。去探访传送门,或阅读制图台。", "你知道的传送门都在附近(已隐藏)。", "还没有收藏的传送门。", "没有符合搜索的传送门。", "新传送门:$1", "传送门 $1 已消失。已从列表中移除。", "这里已经没有传送门了。已从列表中移除。", "从制图台了解的传送门:$1", "记录到制图台的新传送门:$1", "已记录的传送门:$1",
            "在地图上选择一个传送门", "点击", "右键点击", "排序:最近", "排序:最远", "已忽略", "忽略", "没有已忽略的传送门。", "已忽略 $1,可在“已忽略”标签页中找到。", "重命名两端",
            "另一端传送门不可达:只重命名了这一个。",
            "重命名", "此传送门不在范围内：前往该传送门后才能重命名。", "仅传送门", "中键单击", "未命名的传送门已忽略，可在“已忽略”标签页中找到。",
            "传送门名称",
        },
        ["Chinese_Trad"] = new[]
        {
            "傳送門", "出發地:$1", "搜尋...", "全部", "收藏", "排序:名稱", "地圖", "列表", "關閉", "已知傳送門:$1",
            "附近已隱藏:$1", "透過探索或在製圖桌了解更多", "傳送", "收藏", "分頁", "選擇", "下一個傳送門", "取消", "傳送到...", "未命名傳送門",
            "你還不知道其他傳送門。去探訪傳送門,或閱讀製圖桌。", "你知道的傳送門都在附近(已隱藏)。", "還沒有收藏的傳送門。", "沒有符合搜尋的傳送門。", "新傳送門:$1", "傳送門 $1 已消失。已從清單中移除。", "這裡已經沒有傳送門了。已從清單中移除。", "從製圖桌了解的傳送門:$1", "記錄到製圖桌的新傳送門:$1", "已記錄的傳送門:$1",
            "在地圖上選擇一個傳送門", "點擊", "右鍵點擊", "排序:最近", "排序:最遠", "已忽略", "忽略", "沒有已忽略的傳送門。", "已忽略 $1,可在「已忽略」分頁中找到。", "重新命名兩端",
            "另一端傳送門無法觸及:只重新命名了這一個。",
            "重新命名", "此傳送門不在範圍內：前往該傳送門後才能重新命名。", "僅傳送門", "中鍵點擊", "未命名的傳送門已忽略，可在「已忽略」分頁中找到。",
            "傳送門名稱",
        },
        ["Japanese"] = new[]
        {
            "ポータル", "出発地:$1", "検索...", "すべて", "お気に入り", "並び順:名前", "マップ", "リスト", "閉じる", "既知のポータル:$1",
            "近くで非表示:$1", "探索するか製図台でさらに覚えましょう", "移動", "お気に入り", "タブ", "選択", "次のポータル", "キャンセル", "移動先...", "名前のないポータル",
            "まだ他のポータルを知りません。ポータルを訪れるか、製図台を読んでください。", "知っているポータルはすべて近くにあります(非表示)。", "お気に入りのポータルはまだありません。", "検索に一致するポータルはありません。", "新しいポータル:$1", "ポータル $1 は消えました。リストから削除しました。", "ここにはもうポータルがありません。リストから削除しました。", "製図台から覚えたポータル:$1", "製図台に記録した新しいポータル:$1", "記録済みのポータル:$1",
            "マップでポータルを選んでください", "クリック", "右クリック", "並び順:近い順", "並び順:遠い順", "無視中", "無視", "無視しているポータルはありません。", "$1 を無視しました。「無視中」タブにあります。", "両端の名前を変更",
            "もう一方のポータルに届きません:このポータルだけ名前を変更しました。",
            "名前を変更", "このポータルは範囲外です。名前を変更するには訪れてください。", "ポータルのみ", "中クリック", "名前のないポータルは無視されます。「無視中」タブにあります。",
            "ポータル名",
        },
        ["Korean"] = new[]
        {
            "포털", "출발지: $1", "검색...", "전체", "즐겨찾기", "정렬: 이름", "지도", "목록", "닫기", "알려진 포털: $1",
            "근처 숨김: $1", "탐험하거나 지도 제작대에서 더 알아보세요", "이동", "즐겨찾기", "탭", "선택", "다음 포털", "취소", "이동할 곳...", "이름 없는 포털",
            "아직 다른 포털을 모릅니다. 포털을 방문하거나 지도 제작대를 읽으세요.", "알고 있는 포털이 모두 근처에 있습니다(숨김).", "아직 즐겨찾기한 포털이 없습니다.", "검색과 일치하는 포털이 없습니다.", "새 포털: $1", "포털 $1이(가) 사라졌습니다. 목록에서 제거했습니다.", "여기에는 더 이상 포털이 없습니다. 목록에서 제거했습니다.", "제작대에서 알게 된 포털: $1", "제작대에 기록한 새 포털: $1", "기록된 포털: $1",
            "지도에서 포털을 선택하세요", "클릭", "오른쪽 클릭", "정렬: 가까운 순", "정렬: 먼 순", "무시됨", "무시", "무시한 포털이 없습니다.", "$1을(를) 무시합니다. 무시됨 탭에 있습니다.", "양쪽 이름 바꾸기",
            "다른 포털에 닿을 수 없어 이 포털만 이름을 바꿨습니다.",
            "이름 변경", "이 포털은 범위 밖에 있습니다. 이름을 바꾸려면 방문하세요.", "포털만", "가운데 클릭", "이름 없는 포털은 무시됩니다. 무시됨 탭에 있습니다.",
            "포털 이름",
        },
    };

    static string tableOf;
    static Dictionary<string, string> table;

    /// <summary>The language the mod's texts are in: the Language setting, or the game's on Auto.</summary>
    public static string Language
    {
        get
        {
            string forced = Plugin.Language?.Value;
            if (!string.IsNullOrEmpty(forced) && forced != Auto) return forced;
            return Localization.m_instance != null ? Localization.m_instance.GetSelectedLanguage() : "English";
        }
    }

    /// <summary>The text of <paramref name="key"/> in the mod's language, with <c>$1</c>.. filled in.</summary>
    public static string T(string key, params string[] words)
    {
        string language = Language;
        if (table == null || tableOf != language)
        {
            table = new Dictionary<string, string>();
            foreach (var w in WordsFor(language)) table[w.Key] = w.Value;
            tableOf = language;
        }
        if (!table.TryGetValue(key, out var text)) return key;
        for (int i = 0; i < words.Length; i++) text = text.Replace("$" + (i + 1), words[i]);
        return text;
    }

    /// <summary>The texts of <paramref name="language"/> by key, English where it has none.</summary>
    public static IEnumerable<KeyValuePair<string, string>> WordsFor(string language)
    {
        var english = Rows["English"];
        Rows.TryGetValue(language ?? "", out var row);
        for (int i = 0; i < Keys.Length; i++)
        {
            string text = row != null && i < row.Length && !string.IsNullOrEmpty(row[i]) ? row[i] : english[i];
            yield return new KeyValuePair<string, string>(Keys[i], text);
        }
    }
}
