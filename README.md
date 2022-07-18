# Mensatt Scraper

![Build badge](https://github.com/mensatt/scraper/actions/workflows/dotnet.yml/badge.svg)

Scraping utility to supply [mensatt](https://mensatt.de) with fresh dishes 🍕

## Solution structure

There are two projects included in the solution:

1. **MensattScraper**, the scraping tool itself
2. **MensattScraper.Tests**, various tests to improve reliability

### Requirements

* **[Build]** Dotnet Core >= 6.0
* **[Runtime]** Postgres database
* **[Runtime]** Werkswelt api url for fetching the menu
* **[Runtime, Optional]** Webhook url for providing status updates
* **[Runtime, Optional]** Docker

### Building

Building should be as simple as:

````shell
git clone https://github.com/mensatt/scraper.git
cd scraper/
dotnet build
````

### Testing

xUnit is used as a testing framework, which integrates nicely with dotnet.
Testing your solution can be done by running ``dotnet test`` in the sources root.

### Running

If you want to run the scraper yourself, please ensure that the following environment variables are set:

* ``MENSATT_SCRAPER_API_URL`` - url to a valid xml menu file
* ``MENSATT_SCRAPER_DB`` - [database connection string](https://www.npgsql.org/doc/connection-string-parameters.html),
  as required by Npgsql

Logs are written to console output by default, but you can mirror those messages using a supported webhook.
For this the following environment variable needs to be defined:

* ``MENSATT_SCRAPER_WEBHKOOK`` - url to post a webhook to

You can either run the scraper from source by executing ``dotnet run`` or build the solution and run its generated artefacts.
Keep in mind that you can only run the resulting executables on the same platform as they were built on.

## Contributing

We appreciate all contributions, whether it is through bug reports or pull requests.
Please keep the following things in mind:
* If you find a bug, please check whether this bug was already reported in the [issue tracker](https://github.com/mensatt/scraper/issues) 
* If you want to contribute code changes, please adhere to the following workflow:
1. Fork and clone the repository
2. Create a new branch
3. Make your changes, fix some bugs 🪲🔨
4. Check whether everything builds successfully and **run the unit tests**
5. Push your branch to your repository and submit a pull request against the scraper/main branch
6. If the main branch was updated before your change was merged, please rebase your branch to integrate the new changes using ``git rebase upstream/main``
7. After your change has been merged, you can safely delete your local branch

#### Styling

Try to keep your code style similar to the existing code.