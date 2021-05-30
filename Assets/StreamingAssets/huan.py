with open("Text_Content.json", "r") as read:
    with open("TextJson.txt", "w") as write:
        while(line := read.readline()):
            newline = line.replace("\n", "")
            newline = newline.replace("\t", "")
            newline = newline.replace("\b", "")
            newline = newline.replace(" ", "")
            newline = newline.replace("\"", "\\"+"\"")
            write.write(newline)
            print(newline)
            