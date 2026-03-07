Для поиска коллекции по имени нужно использовать:
        var collections = await _httpClient.GetFromJsonAsync<List<ChromaCollection>>(
    "/api/v2/tenants/default_tenant/databases/default_database/collections"
);
        var collection = collections.FirstOrDefault(c => c.Name == "documents");

 Узнать количество коллекций по Id:
 curl http://localhost:8000/api/v2/tenants/default_tenant/databases/default_database/collections/29c342ef-8b1e-49b8-90ab-b4dababce32f/count       

 Как найти коллекцию:
 1. Вытянуть все коллекции
 2. Найти по имени свою и запомнить Id
 3. Искать в ChromeDB по Id коллекции