import pandas as pd
import matplotlib.pyplot as plt
data = pd.read_csv("../../data/out/log.csv")

plt.figure(figsize=(10,6))
plt.plot(data["Time"], data["Foxes"], label="Foxes")
plt.plot(data["Time"], data["Rabbits"], label="Rabbits")
plt.xlabel("Time")
plt.ylabel("Population")
plt.title("Organism populations")
plt.legend()
plt.grid()

plt.show()