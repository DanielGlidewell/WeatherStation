# Personal Weather Station
Measure environmental parameters (temperature, humidity, pressure, and altitude) using a Raspberry Pi and stream the data to the Azure cloud so that multiple consumers can leverage them in real time.

## Overview
The goal of this project is to get acquainted with Microsoft's IoT ecosystem, and is essentially an amalgamation of the things I've learned from various sources around the Internet on the subject. I took the things that I liked from other projects and modified where necessary until I was happy with the outcome. See the sources below for links to the hard work others did so that I could learn. 

The weather station is designed to sample environmental parameters - temperature, humidity, pressure, and altitude and send that data directly to an Azure Event Hub. From there the data is consumed by downstream processes which monitor the data for changes and display the data in a real-time dashboard. 

## Technologies
* Raspberry Pi 3 Model B v1.2
* BME280 (Found at adafruit.com)
* Windows IoT Core
* C# - Universal Windows Platform
* Azure Event Hubs
* Streaming Analytics (Not Yet Implemented)
* Azure Functions (Not Yet Implemented)
* Power BI (Not Yet Implemented)

## Sources
* https://www.hackster.io/windows-iot/weather-station-v-2-0-8abe16
* https://github.com/ms-iot/adafruitsample
* https://app.pluralsight.com/library/courses/azure-event-hubs-dotnet-developers-fundamentals/table-of-contents
* https://www.youtube.com/watch?v=A-kazyOiBvs&t=1s
